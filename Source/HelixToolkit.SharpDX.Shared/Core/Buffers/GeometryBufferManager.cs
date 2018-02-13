﻿/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
//#define DEBUGDETAIL
using System;
using System.Collections.Generic;
using System.Diagnostics;

#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX.Core
#else
namespace HelixToolkit.UWP.Core
#endif
{
    using Utilities;
    /// <summary>
    /// Use to manage geometry vertex/index buffers. 
    /// Same geometry with same buffer type will share the same buffer across all models.
    /// </summary>
    public sealed class GeometryBufferManager : DisposeObject, IGeometryBufferManager
    {
        /// <summary>
        /// The buffer dictionary. Key1=<see cref="Geometry3D.GUID"/>, Key2=Typeof(Buffer)
        /// </summary>
        private readonly DoubleKeyDictionary<Type, Guid, GeometryBufferContainer> bufferDictionary
            = new DoubleKeyDictionary<Type, Guid, GeometryBufferContainer>();

        /// <summary>
        /// Initializes a new instance of the <see cref="GeometryBufferManager"/> class.
        /// </summary>
        public GeometryBufferManager()
        {

        }
        /// <summary>
        /// Registers the specified model unique identifier.
        /// </summary>
        /// <typeparam name="T">Geometry Buffer Type</typeparam>
        /// <param name="modelGuid">The model unique identifier.</param>
        /// <param name="geometry">The geometry.</param>
        /// <returns></returns>
        public IGeometryBufferModel Register<T>(Guid modelGuid, Geometry3D geometry) where T:IGeometryBufferModel
        {
            if (geometry == null || modelGuid == Guid.Empty)
            {
                return new EmptyGeometryBufferModel();
            }
            lock (bufferDictionary)
            {
                GeometryBufferContainer container = null;
                if(bufferDictionary.TryGetValue(typeof(T), geometry.GUID, out container))
                {
#if DEBUGDETAIL
                    Debug.WriteLine("Existing buffer found, GeomoetryGUID = " + geometry.GUID);
#endif
                    container.Attach(modelGuid);
                }
                else
                {
#if DEBUGDETAIL
                    Debug.WriteLine("Buffer not found, create new buffer. GeomoetryGUID = " + geometry.GUID);
#endif
                    container = GeometryBufferContainer.Create<T>();
                    var id = geometry.GUID;
                    container.Disposed += (s, e) => 
                    {
                        bufferDictionary.Remove(typeof(T), id);
                    };
                    container.Buffer.Geometry = geometry;
                    container.Attach(modelGuid);
                    bufferDictionary.Add(typeof(T), geometry.GUID, container);
                
                }
                return container.Buffer;
            }
        }
        /// <summary>
        /// Unregisters the specified model unique identifier.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="modelGuid">The model unique identifier.</param>
        /// <param name="geometry">The geometry.</param>
        /// <returns></returns>
        public bool Unregister<T>(Guid modelGuid, Geometry3D geometry) where T:IGeometryBufferModel
        {
            if (geometry == null || modelGuid == Guid.Empty)
            {
                return false;
            }
            lock (bufferDictionary)
            {
                GeometryBufferContainer container = null;
                if(bufferDictionary.TryGetValue(typeof(T), geometry.GUID, out container))
                {
#if DEBUGDETAIL
                    Debug.WriteLine("Existing buffer found, Detach model from buffer. ModelGUID = " + modelGuid);
#endif
                    container.Detach(modelGuid);
                    return true;
                }
                else
                {
                    Debug.WriteLine("Unregister geometry buffer, buffer is not found.");
                    return false;
                }
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposeManagedResources"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposeManagedResources)
        {
            if (disposeManagedResources)
            {
                foreach (var buffer in bufferDictionary.Values)
                {
                    buffer.Dispose();
                }
                bufferDictionary.Clear();
            }
            base.Dispose(disposeManagedResources);
        }

        /// <summary>
        /// 
        /// </summary>
        private sealed class GeometryBufferContainer : ResourceSharedObject
        {
            private readonly IGeometryBufferModel buffer;
            /// <summary>
            /// Gets the buffer.
            /// </summary>
            /// <value>
            /// The buffer.
            /// </value>
            public IGeometryBufferModel Buffer { get { return buffer; } }
            /// <summary>
            /// Initializes a new instance of the <see cref="GeometryBufferContainer"/> class.
            /// </summary>
            /// <param name="model">The model.</param>
            private GeometryBufferContainer(IGeometryBufferModel model)
            {
                buffer = Collect(model);
            }

            /// <summary>
            /// Creates the specified structure size.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            public static GeometryBufferContainer Create<T>() where T : IGeometryBufferModel
            {
                var buffer = Activator.CreateInstance(typeof(T)) as IGeometryBufferModel;
                return new GeometryBufferContainer(buffer);
            }
        }
    }
}
