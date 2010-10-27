﻿#region License
//
// (C) Copyright 2009 Patrick Cozzi and Deron Ohlarik
//
// Distributed under the Boost Software License, Version 1.0.
// See License.txt or http://www.boost.org/LICENSE_1_0.txt.
//
#endregion

namespace OpenGlobe.Renderer
{
    public abstract class TextureUnit
    {
        public abstract Texture2D Texture2D { get; set; }
        public abstract Texture2D Texture2DRectangle { get; set; }
        public abstract TextureSampler TextureSampler { get; set; }
    }
}
