﻿#region License
//
// (C) Copyright 2009 Patrick Cozzi and Deron Ohlarik
//
// Distributed under the Boost Software License, Version 1.0.
// See License.txt or http://www.boost.org/LICENSE_1_0.txt.
//
#endregion

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using MiniGlobe.Core;
using ImagingPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace MiniGlobe.Renderer.GL3x
{
    internal class BufferGL3x
    {
        public BufferGL3x(
            BufferTarget type, 
            BufferHint usageHint, 
            int sizeInBytes)
        {
            Debug.Assert(sizeInBytes > 0);

            GL.GenBuffers(1, out _handle);
            _sizeInBytes = sizeInBytes;
            _type = type;
            _usageHint = TypeConverterGL3x.To(usageHint);

            //
            // Allocating here with GL.BufferData, then writing with GL.BufferSubData
            // in CopyFromSystemMemory() should not have any serious overhead:
            //
            //   http://www.opengl.org/discussion_boards/ubbthreads.php?ubb=showflat&Number=267373#Post267373
            //
            // Alternately, we can delay GL.BufferData until the first
            // CopyFromSystemMemory() call.
            //
            Bind();
            GL.BufferData(_type, new IntPtr(sizeInBytes), new IntPtr(), _usageHint);
        }

        public void CopyFromSystemMemory<T>(
            T[] bufferInSystemMemory,
            int destinationOffsetInBytes,
            int lengthInBytes) where T : struct
        {
            Debug.Assert(destinationOffsetInBytes >= 0);
            Debug.Assert(destinationOffsetInBytes + lengthInBytes <= _sizeInBytes);

            Debug.Assert(lengthInBytes >= 0);
            Debug.Assert(lengthInBytes <= bufferInSystemMemory.Length * SizeInBytes<T>.Value);

            Bind();
            GL.BufferSubData<T>(_type,
                new IntPtr(destinationOffsetInBytes),
                new IntPtr(lengthInBytes),
                bufferInSystemMemory);
        }

        public void CopyFromBitmap(Bitmap bitmap)
        {
            //
            // OpenGL wants rows bottom to top.
            //
            Bitmap flippedBitmap = bitmap.Clone() as Bitmap;
            flippedBitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            
            BitmapData lockedPixels = flippedBitmap.LockBits(new Rectangle(
                0, 0, flippedBitmap.Width, flippedBitmap.Height),
                ImageLockMode.ReadOnly, flippedBitmap.PixelFormat);

            // TODO:  Does the row have padding?  e.g. 4 byte aligned, not tightly packed
            int sizeInBytes = lockedPixels.Stride * lockedPixels.Height;

            Bind();
            GL.BufferSubData(_type,
                new IntPtr(),
                new IntPtr(sizeInBytes),
                lockedPixels.Scan0);

            flippedBitmap.UnlockBits(lockedPixels);
        }

        public T[] CopyToSystemMemory<T>(int offsetInBytes, int lengthInBytes) where T : struct
        {
            Debug.Assert(offsetInBytes >= 0);
            Debug.Assert(lengthInBytes > 0);
            Debug.Assert(offsetInBytes + lengthInBytes <= _sizeInBytes);

            T[] bufferInSystemMemory = new T[lengthInBytes / SizeInBytes<T>.Value];

            Bind();
            GL.GetBufferSubData(_type, new IntPtr(offsetInBytes), new IntPtr(lengthInBytes), bufferInSystemMemory);
            return bufferInSystemMemory;
        }

        public Bitmap CopyToBitmap(int width, int height, ImagingPixelFormat pixelFormat)
        {
            Debug.Assert(width > 0);
            Debug.Assert(height > 0);

            Bitmap bitmap = new Bitmap(width, height, pixelFormat);

            BitmapData lockedPixels = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly, bitmap.PixelFormat);

            // TODO:  Does the row have padding?  e.g. 4 byte aligned, not tightly packed
            int sizeInBytes = lockedPixels.Stride * lockedPixels.Height;

            // TODO:  If sizeInBytes is wrong because of either the format or row padding 
            // (above), GetBufferSubData will throw InvalidEnum.
            Bind();
            GL.GetBufferSubData(_type, new IntPtr(), new IntPtr(sizeInBytes), lockedPixels.Scan0);

            bitmap.UnlockBits(lockedPixels);

            //
            // OpenGL had rows bottom to top.  Bitmap wants them top to bottom.
            //
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

            return bitmap;
        }

        public int SizeInBytes
        {
            get { return _sizeInBytes; }
        }

        public BufferHint UsageHint
        {
            get { return TypeConverterGL3x.To(_usageHint); }
        }

        internal int Handle
        {
            get { return _handle; }
        }

        internal void Bind()
        {
            // TODO: avoid duplicate binds
            GL.BindBuffer(_type, _handle);
        }

        internal void Dispose(bool disposing)
        {
            if (disposing)
            {
                GL.DeleteBuffers(1, ref _handle);
            }
        }

        private int _handle;
        private readonly int _sizeInBytes;
        private readonly BufferTarget _type;
        private readonly BufferUsageHint _usageHint;
    }
}