﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLRig.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SharpTL
{
    /// <summary>
    ///     Type Language tooling equipment.
    /// </summary>
    public class TLRig
    {
        /// <summary>
        ///     Default instance of the <see cref="TLRig" /> class.
        /// </summary>
        public static readonly TLRig Default = new TLRig();

        private readonly TLSerializersBucket _serializersBucket;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TLRig" /> class.
        /// </summary>
        public TLRig()
        {
            _serializersBucket = new TLSerializersBucket();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TLRig" /> class.
        /// </summary>
        /// <param name="serializersBucket">Serializers bucket.</param>
        public TLRig(TLSerializersBucket serializersBucket)
        {
            _serializersBucket = serializersBucket;
        }

        /// <summary>
        ///     Get serializer.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <returns>Serializer.</returns>
        public ITLSerializer GetSerializer<T>()
        {
            return GetSerializerByObjectType(typeof (T));
        }

        /// <summary>
        ///     Get serializer by object type.
        /// </summary>
        /// <param name="objType">Object type.</param>
        /// <returns>Serializer.</returns>
        public ITLSerializer GetSerializerByObjectType(Type objType)
        {
            return _serializersBucket[objType];
        }

        /// <summary>
        ///     Get serializer by constructor number.
        /// </summary>
        /// <param name="constructorNumber">Constructor number.</param>
        /// <returns>Serializer.</returns>
        public ITLSerializer GetSerializerByConstructorNumber(uint constructorNumber)
        {
            return _serializersBucket[constructorNumber];
        }

        /// <summary>
        ///     Serializer an object.
        /// </summary>
        /// <param name="obj">The object to be serialized.</param>
        /// <param name="stream">Stream for writing.</param>
        /// <param name="modeOverride">Serialization mode override.</param>
        public void Serialize(object obj, Stream stream, TLSerializationMode? modeOverride = null)
        {
            using (var streamer = new TLStreamer(stream))
            {
                Serialize(obj, new TLSerializationContext(this, streamer), modeOverride);
            }
        }

        /// <summary>
        ///     Serializer an object.
        /// </summary>
        /// <param name="obj">The object to be serialized.</param>
        /// <param name="modeOverride">Serialization mode override.</param>
        /// <returns>Serialized object in array of bytes.</returns>
        public byte[] Serialize(object obj, TLSerializationMode? modeOverride = null)
        {
            using (var stream = new MemoryStream())
            {
                Serialize(obj, stream, modeOverride);
                return stream.ToArray();
            }
        }

        /// <summary>
        ///     Deserialize an object.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="objBytes">Bytes for reading.</param>
        /// <param name="modeOverride">Serialization mode override.</param>
        /// <returns>Deserialized object.</returns>
        public T Deserialize<T>(byte[] objBytes, TLSerializationMode? modeOverride = null)
        {
            using (var stream = new MemoryStream(objBytes))
            {
                return Deserialize<T>(stream, modeOverride);
            }
        }

        /// <summary>
        ///     Deserialize an object.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="stream">Stream for reading.</param>
        /// <param name="modeOverride">Serialization mode override.</param>
        /// <returns>Deserialized object.</returns>
        public T Deserialize<T>(Stream stream, TLSerializationMode? modeOverride = null)
        {
            return (T) Deserialize(stream, typeof (T), modeOverride);
        }

        /// <summary>
        ///     Deserialize an object.
        /// </summary>
        /// <param name="objBytes">Bytes for reading.</param>
        /// <param name="objType">Type of the object.</param>
        /// <param name="modeOverride">Serialization mode override.</param>
        /// <returns>Deserialized object.</returns>
        public object Deserialize(byte[] objBytes, Type objType, TLSerializationMode? modeOverride = null)
        {
            using (var stream = new MemoryStream(objBytes))
            {
                return Deserialize(stream, objType, modeOverride);
            }
        }

        /// <summary>
        ///     Deserialize an object.
        /// </summary>
        /// <param name="stream">Stream for reading.</param>
        /// <param name="objType">Type of the object.</param>
        /// <param name="modeOverride">Serialization mode override.</param>
        /// <returns>Deserialized object.</returns>
        public object Deserialize(Stream stream, Type objType, TLSerializationMode? modeOverride = null)
        {
            using (var streamer = new TLStreamer(stream))
            {
                return Deserialize(objType, new TLSerializationContext(this, streamer), modeOverride);
            }
        }

        /// <summary>
        ///     Deserialize an object.
        /// </summary>
        /// <param name="objBytes">Bytes for reading.</param>
        /// <returns>Deserialized object.</returns>
        public object Deserialize(byte[] objBytes)
        {
            using (var stream = new MemoryStream(objBytes))
            {
                return Deserialize(stream);
            }
        }

        /// <summary>
        ///     Deserialize an object.
        /// </summary>
        /// <param name="stream">Stream for reading.</param>
        /// <returns>Deserialized object.</returns>
        public object Deserialize(Stream stream)
        {
            using (var streamer = new TLStreamer(stream))
            {
                return Deserialize(new TLSerializationContext(this, streamer));
            }
        }

        /// <summary>
        ///     Prepare serializers for all TL objects in an assembly.
        ///     For all objects with TLObject attribute should be prepared a serializer.
        /// </summary>
        /// <param name="assembly">Assembly with TL objects.</param>
        public void PrepareSerializersForAllTLObjectsInAssembly(Assembly assembly)
        {
            foreach (TypeInfo typeInfo in assembly.DefinedTypes.Where(typeInfo => typeInfo.GetCustomAttribute<TLObjectAttribute>() != null))
            {
                _serializersBucket.PrepareSerializer(typeInfo.AsType());
            }
        }

        /// <summary>
        ///     Serialize an object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="context">TL serialization context.</param>
        /// <param name="modeOverride">Serialization mode override.</param>
        /// <exception cref="TLSerializerNotFoundException">When serializer not found.</exception>
        public static void Serialize(object obj, TLSerializationContext context, TLSerializationMode? modeOverride = null)
        {
            var objType = obj.GetType();
            ITLSerializer serializer = context.Rig.GetSerializerByObjectType(objType);
            if (serializer == null)
            {
                throw new TLSerializerNotFoundException(string.Format("There is no serializer for a type: '{0}'.", objType.FullName));
            }
            serializer.Write(obj, context, modeOverride);
        }

        /// <summary>
        ///     Deserialize an object from TL serialization context.
        /// </summary>
        /// <remarks>
        ///     Constructor number for the object is automatically determined by reading the first number from the streamer,
        ///     hence object within the context streamer must be serialized as boxed type.
        /// </remarks>
        /// <param name="context">TL serialization context.</param>
        /// <returns>Deserialized object.</returns>
        /// <exception cref="TLSerializerNotFoundException">When serializer not found.</exception>
        public static object Deserialize(TLSerializationContext context)
        {
            TLStreamer streamer = context.Streamer;

            // Read a constructor number.
            uint constructorNumber = streamer.ReadUInt32();
            ITLSerializer serializer = context.Rig.GetSerializerByConstructorNumber(constructorNumber);
            if (serializer == null)
            {
                throw new TLSerializerNotFoundException(string.Format("Constructor number: 0x{0:X8} is not supported by any registered serializer.", constructorNumber));
            }

            // Bare because construction number has already been read.
            return serializer.Read(context, TLSerializationMode.Bare);
        }

        /// <summary>
        ///     Deserialize an object from TL serialization context.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="context">TL serialization context.</param>
        /// <param name="modeOverride">Serialization mode override.</param>
        /// <returns>Deserialized object.</returns>
        public static T Deserialize<T>(TLSerializationContext context, TLSerializationMode? modeOverride = null)
        {
            return (T) Deserialize(typeof (T), context, modeOverride);
        }

        /// <summary>
        ///     Deserialize an object from TL serialization context.
        /// </summary>
        /// <param name="objType">Type of the object.</param>
        /// <param name="context">TL serialization context.</param>
        /// <param name="modeOverride">Serialization mode override.</param>
        /// <returns>Deserialized object.</returns>
        /// <exception cref="TLSerializerNotFoundException">When serializer not found.</exception>
        public static object Deserialize(Type objType, TLSerializationContext context, TLSerializationMode? modeOverride = null)
        {
            if (objType == typeof (object))
            {
                return Deserialize(context);
            }

            ITLSerializer serializer = context.Rig.GetSerializerByObjectType(objType);
            if (serializer == null)
            {
                throw new TLSerializerNotFoundException(string.Format("There is no serializer for a type: '{0}'.", objType.FullName));
            }
            return serializer.Read(context, modeOverride);
        }
    }
}
