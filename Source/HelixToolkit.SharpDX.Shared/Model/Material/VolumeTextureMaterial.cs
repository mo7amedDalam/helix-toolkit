﻿/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
using SharpDX;
#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX.Model
#else
namespace HelixToolkit.UWP.Model
#endif
{
    using Shaders;
    using System.IO;
    using Utilities;

    public struct VolumeTextureParams
    {
        public byte[] VolumeTextures;
        public int Width;
        public int Height;
        public int Depth;
        public global::SharpDX.DXGI.Format Format;
    }

    public abstract class VolumeTextureMaterialBase<T> : MaterialCore
    {
        private T volumeTexture;
        public T VolumeTexture
        {
            set { Set(ref volumeTexture, value); }
            get { return volumeTexture; }
        }

        private global::SharpDX.Direct3D11.SamplerStateDescription sampler = DefaultSamplers.LinearSamplerClampAni1;
        public global::SharpDX.Direct3D11.SamplerStateDescription Sampler
        {
            set { Set(ref sampler, value); }
            get { return sampler; }
        }

        private float stepSize = 0.1f;
        public float StepSize
        {
            set { Set(ref stepSize, value); }
            get { return stepSize; }
        }

        private int iteration = 10;
        public int Iteration
        {
            set { Set(ref iteration, value); }
            get { return iteration; }
        }

        private Color4 color = new Color4(1,1,1,1);
        public Color4 Color
        {
            set { Set(ref color, value); }
            get { return color; }
        }

        public override MaterialVariable CreateMaterialVariables(IEffectsManager manager, IRenderTechnique technique)
        {
            return new VolumeMaterialVariable<T>(manager, technique, this)
            {
                OnCreateTexture = (material, effectsManager) => { return OnCreateTexture(effectsManager); }
            };
        }

        protected abstract ShaderResourceViewProxy OnCreateTexture(IEffectsManager manager);
    }


    public sealed class VolumeTextureMaterial : VolumeTextureMaterialBase<Stream>
    {
        protected override ShaderResourceViewProxy OnCreateTexture(IEffectsManager manager)
        {
            return manager.MaterialTextureManager.Register(VolumeTexture);
        }
    }

    public sealed class RawDataVolumeTextureMaterial : VolumeTextureMaterialBase<VolumeTextureParams>
    {
        protected override ShaderResourceViewProxy OnCreateTexture(IEffectsManager manager)
        {
            return ShaderResourceViewProxy.CreateViewFromPixelData(manager.Device, VolumeTexture.VolumeTextures,
                VolumeTexture.Width, VolumeTexture.Height, VolumeTexture.Depth, VolumeTexture.Format);
        }

        public static VolumeTextureParams LoadRAWFile(string filename, int width, int height, int depth)
        {
            using (FileStream file = new FileStream(filename, FileMode.Open))
            {
                long length = file.Length;
                var bytePerPixel = length / (width * height * depth);
                byte[] buffer = new byte[width * height * depth * bytePerPixel];
                using (BinaryReader reader = new BinaryReader(file))
                {                   
                    reader.Read(buffer, 0, buffer.Length);
                }
                var ret = new VolumeTextureParams()
                {
                    VolumeTextures = buffer,
                    Width = width,
                    Height = height,
                    Depth = depth,
                };
                switch (bytePerPixel)
                {
                    case 1:
                        ret.Format = global::SharpDX.DXGI.Format.R8_UNorm;
                        break;
                    case 2:
                        ret.Format = global::SharpDX.DXGI.Format.R16_UNorm;
                        break;
                    case 4:
                        ret.Format = global::SharpDX.DXGI.Format.R32_Float;
                        break;
                }
                return ret;
            }              
        }
    }
}
