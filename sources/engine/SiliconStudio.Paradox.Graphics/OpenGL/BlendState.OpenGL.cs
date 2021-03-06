// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGL 
using System;
#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
using OpenTK.Graphics.ES30;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace SiliconStudio.Paradox.Graphics
{
    public partial class BlendState
    {
        private readonly bool blendEnable;
        private readonly BlendEquationMode blendEquationModeColor;
        private readonly BlendEquationMode blendEquationModeAlpha;
        private readonly BlendingFactorSrc blendFactorSrcColor;
        private readonly BlendingFactorSrc blendFactorSrcAlpha;
        private readonly BlendingFactorDest blendFactorDestColor;
        private readonly BlendingFactorDest blendFactorDestAlpha;
        private readonly uint blendEquationHash;
        private readonly uint blendFuncHash;
        internal readonly bool[] EnabledColors = new bool[4];

        internal BlendState(GraphicsDevice device, BlendStateDescription blendStateDescription) : base(device)
        {
            Description = blendStateDescription;

            for (int i = 1; i < Description.RenderTargets.Length; ++i)
            {
                if (Description.RenderTargets[i].BlendEnable || Description.RenderTargets[i].ColorWriteChannels != ColorWriteChannels.All)
                    throw new NotSupportedException();
            }

            blendEnable = Description.RenderTargets[0].BlendEnable;

            blendEquationModeColor = ToOpenGL(Description.RenderTargets[0].ColorBlendFunction);
            blendEquationModeAlpha = ToOpenGL(Description.RenderTargets[0].AlphaBlendFunction);
            blendFactorSrcColor = ToOpenGL(Description.RenderTargets[0].ColorSourceBlend);
            blendFactorSrcAlpha = ToOpenGL(Description.RenderTargets[0].AlphaSourceBlend);
            blendFactorDestColor = (BlendingFactorDest)ToOpenGL(Description.RenderTargets[0].ColorDestinationBlend);
            blendFactorDestAlpha = (BlendingFactorDest)ToOpenGL(Description.RenderTargets[0].AlphaDestinationBlend);
            EnabledColors[0] = (Description.RenderTargets[0].ColorWriteChannels & ColorWriteChannels.Red  ) != 0;
            EnabledColors[1] = (Description.RenderTargets[0].ColorWriteChannels & ColorWriteChannels.Green) != 0;
            EnabledColors[2] = (Description.RenderTargets[0].ColorWriteChannels & ColorWriteChannels.Blue ) != 0;
            EnabledColors[3] = (Description.RenderTargets[0].ColorWriteChannels & ColorWriteChannels.Alpha) != 0;

            blendEquationHash = (uint)Description.RenderTargets[0].ColorBlendFunction
                             | ((uint)Description.RenderTargets[0].AlphaBlendFunction << 8);

            blendFuncHash = (uint)Description.RenderTargets[0].ColorSourceBlend
                         | ((uint)Description.RenderTargets[0].AlphaSourceBlend << 8)
                         | ((uint)Description.RenderTargets[0].ColorDestinationBlend << 16)
                         | ((uint)Description.RenderTargets[0].AlphaDestinationBlend << 24);
        }

        /// <inheritdoc/>
        protected internal override bool OnRecreate()
        {
            base.OnRecreate();
            return true;
        }

        public static BlendEquationMode ToOpenGL(BlendFunction blendFunction)
        {
            switch (blendFunction)
            {
                case BlendFunction.Subtract:
                    return BlendEquationMode.FuncSubtract;
                case BlendFunction.Add:
                    return BlendEquationMode.FuncAdd;
#if !SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                case BlendFunction.Max:
                    return BlendEquationMode.Max;
                case BlendFunction.Min:
                    return BlendEquationMode.Min;
#endif
                case BlendFunction.ReverseSubtract:
                    return BlendEquationMode.FuncReverseSubtract;
                default:
                    throw new NotSupportedException();
            }
        }

        public static BlendingFactorSrc ToOpenGL(Blend blend)
        {
            switch (blend)
            {
                case Blend.Zero:
                    return BlendingFactorSrc.Zero;
                case Blend.One:
                    return BlendingFactorSrc.One;
                case Blend.SourceColor:
                    return (BlendingFactorSrc)BlendingFactorDest.SrcColor;
                case Blend.InverseSourceColor:
                    return (BlendingFactorSrc)BlendingFactorDest.OneMinusSrcColor;
                case Blend.SourceAlpha:
                    return BlendingFactorSrc.SrcAlpha;
                case Blend.InverseSourceAlpha:
                    return BlendingFactorSrc.OneMinusSrcAlpha;
                case Blend.DestinationAlpha:
                    return BlendingFactorSrc.DstAlpha;
                case Blend.InverseDestinationAlpha:
                    return BlendingFactorSrc.OneMinusDstAlpha;
                case Blend.DestinationColor:
                    return BlendingFactorSrc.DstColor;
                case Blend.InverseDestinationColor:
                    return BlendingFactorSrc.OneMinusDstColor;
                case Blend.SourceAlphaSaturate:
                    return BlendingFactorSrc.SrcAlphaSaturate;
                case Blend.BlendFactor:
                    return BlendingFactorSrc.ConstantColor;
                case Blend.InverseBlendFactor:
                    return BlendingFactorSrc.OneMinusConstantColor;
                case Blend.SecondarySourceColor:
                case Blend.InverseSecondarySourceColor:
                case Blend.SecondarySourceAlpha:
                case Blend.InverseSecondarySourceAlpha:
                    throw new NotSupportedException();
                default:
                    throw new ArgumentOutOfRangeException("blend");
            }
        }

        public void Apply(BlendState oldBlendState)
        {
            // note: need to update blend equation, blend function and color mask even when the blend state is disable in order to keep the hash based caching system valid

            if (blendEnable && !oldBlendState.blendEnable)
                GL.Enable(EnableCap.Blend);

            if (blendEquationHash != oldBlendState.blendEquationHash)
                GL.BlendEquationSeparate(blendEquationModeColor, blendEquationModeAlpha);

            if (blendFuncHash != oldBlendState.blendFuncHash)
                GL.BlendFuncSeparate(blendFactorSrcColor, blendFactorDestColor, blendFactorSrcAlpha, blendFactorDestAlpha);

            if (Description.RenderTargets[0].ColorWriteChannels != oldBlendState.Description.RenderTargets[0].ColorWriteChannels)
                ApplyColorMask();

            if(!blendEnable && oldBlendState.blendEnable)
                GL.Disable(EnableCap.Blend);
        }

        internal void ApplyColorMask()
        {
            bool hasRenderTarget = GraphicsDevice.hasRenderTarget;
            GL.ColorMask(hasRenderTarget && EnabledColors[0], hasRenderTarget && EnabledColors[1], hasRenderTarget && EnabledColors[2], hasRenderTarget && EnabledColors[3]);
        }
    }
} 
#endif
