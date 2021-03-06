﻿using System;
using System.Collections.Generic;
using UnityEngine;
using g3;

namespace f3
{

    /// <summary>
    /// WorldCoords are the absolute values of coordinates after all transforms, after any viewing transforms.
    /// SceneCoords are coordinates "in the scene", ie they are not affected by viewing transforms
    /// ObjectCoords are the local coordinates of an SO. If the SO does not have a parent SO, these
    ///   will be the same as SceneCoords (right?)
    /// </summary>
	public enum CoordSpace {
		WorldCoords = 0,
		SceneCoords = 1,
		ObjectCoords = 2
	};


    /// <summary>
    /// An object in our universe that has a transformation and scale
    /// </summary>
    public interface ITransformed
    {
		Frame3f GetLocalFrame(CoordSpace eSpace);
        Vector3f GetLocalScale();
    }

    /// <summary>
    /// An object in our universe that has an editable transformation and scale
    /// </summary>
	public interface ITransformable : ITransformed
	{
		void SetLocalFrame(Frame3f newFrame, CoordSpace eSpace);
        bool SupportsScaling { get; }
        void SetLocalScale(Vector3f scale);
	}

    /// <summary>
    /// An object that contains other SceneObjects
    /// </summary>
    public interface SOCollection
    {
        IEnumerable<SceneObject> GetChildren();
    }

    /// <summary>
    /// SOParent is used to define our own hierarchy of objects. We 
    /// need a type where the Parent can either be another SO, or the Scene.
    /// Then we can traverse "up" the parent hierarchy until we hit the Scene.
    /// </summary>
    public interface SOParent : ITransformed
    {
    }


	public interface SceneObject
	{
		GameObject RootGameObject { get; }
        SOParent Parent { get; set; }

        string UUID { get; }
        string Name { get; set; }
        int Timestamp { get; }
        SOType Type { get; }

        bool IsTemporary { get; }       // If return true, means object should not be serialized, etc
                                        // Useful for temp parent objects, that kind of thing.

        bool IsSurface { get; }         // does this object have a surface we can use (ie a mesh/etc)

		void SetScene(FScene s);
		FScene GetScene();

        SceneObject Duplicate();

        void SetCurrentTime(double time);   // for keyframing

		void AssignSOMaterial(SOMaterial m);
        SOMaterial GetAssignedSOMaterial();

        void PushOverrideMaterial(Material m);
        void PopOverrideMaterial();
        Material GetActiveMaterial();

        // called on per-frame Update()
        void PreRender();

		bool FindRayIntersection(Ray3f ray, out SORayHit hit);

        AxisAlignedBox3f GetTransformedBoundingBox();
        AxisAlignedBox3f GetLocalBoundingBox();
	}


    // should we just make scene object transformable??
    public delegate void TransformChangedEventHandler(TransformableSO so);
	public interface TransformableSO : SceneObject, ITransformable
	{
        event TransformChangedEventHandler OnTransformModified;
	}




}

