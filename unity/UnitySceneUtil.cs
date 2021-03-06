﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    public static class UnitySceneUtil
    {

        // converts input GameObject into a DMeshSO. Original GameObject is not used by DMeshSO,
        // so it can be destroyed.
        // [TODO] transfer materials!!
        public static TransformableSO WrapMeshGameObject(GameObject wrapGO, FContext context, bool bDestroyOriginal, bool bPreserveColor = true)
        {
            SOMaterial overrideMaterial = null;
            if (bPreserveColor) {
                if (wrapGO.GetMaterial() != null)
                    overrideMaterial = MaterialUtil.FromUnityMaterial(wrapGO.GetMaterial());
            }
            var wrapperSO = ImportExistingUnityMesh(wrapGO, context.Scene, true, true, true,
                        (mesh, material) => {
                            DMeshSO gso = new DMeshSO();
                            var useMaterial = (overrideMaterial != null) ? overrideMaterial : material;
                            return gso.Create(UnityUtil.UnityMeshToDMesh(mesh, false), useMaterial);
                        } );
            if ( bDestroyOriginal )
                GameObject.Destroy(wrapGO);
            return wrapperSO;
        }



        // embeds input GameObject in a GOWrapperSO, which provides some basic
        // F3 functionality for arbitrary game objects
        public static TransformableSO WrapAnyGameObject(GameObject wrapGO, FContext context, bool bAllowMaterialChanges)
        {
            GOWrapperSO wrapperSO = new GOWrapperSO() {
                AllowMaterialChanges = bAllowMaterialChanges
            };
            wrapperSO.Create(wrapGO);
            context.Scene.AddSceneObject(wrapperSO, true);
            return wrapperSO;
        }





        // extracts MeshFilter object from input GameObject and passes it to a custom constructor
        // function MakeSOFunc (if null, creates basic MeshSO). Then optionally adds to Scene,
        // preserving existing 3D position if desired (default true)
        public static TransformableSO ImportExistingUnityMesh(GameObject go, FScene scene, 
            bool bAddToScene = true, bool bKeepWorldPosition = true, bool bRecenterFrame = true,
            Func<Mesh, SOMaterial, TransformableSO> MakeSOFunc = null )
        {
            MeshFilter meshF = go.GetComponent<MeshFilter>();
            if (meshF == null)
                throw new Exception("SceneUtil.ImportExistingUnityMesh: gameObject is not a mesh!!");

            Vector3f scale = go.GetLocalScale();

            Mesh useMesh = meshF.mesh;      // makes a copy
            AxisAlignedBox3f bounds = useMesh.bounds;       // bounds.Center is wrt local frame of input go
                                                            // ie offset from origin in local coordinates

            // if we want to move frame to center of mesh, we have to re-center it at origin
            // in local coordinates
            if (bRecenterFrame) {           
                UnityUtil.TranslateMesh(useMesh, -bounds.Center.x, -bounds.Center.y, -bounds.Center.z);
                useMesh.RecalculateBounds();
            }

            TransformableSO newSO = (MakeSOFunc != null) ? 
                MakeSOFunc(useMesh, scene.DefaultMeshSOMaterial)
                : new MeshSO().Create(useMesh, scene.DefaultMeshSOMaterial);

            if ( bAddToScene )
                scene.AddSceneObject(newSO, false);

            if ( bKeepWorldPosition ) {

                // compute world rotation/location. If we re-centered the mesh, we need
                // to offset by the transform we applied above in local coordinates 
                // (hence we have to rotate & scale)
                if (go.transform.parent != null)
                    throw new Exception("UnitySceneUtil.ImportExistingUnityMesh: Not handling case where GO has a parent transform");
                Frame3f goFrameW = UnityUtil.GetGameObjectFrame(go, CoordSpace.WorldCoords);
                Vector3f originW = goFrameW.Origin;
                if (bRecenterFrame)
                    originW += goFrameW.Rotation * (scale * bounds.Center);   // offset initial frame to be at center of mesh

                // convert world frame and offset to scene coordinates
                Frame3f goFrameS = scene.ToSceneFrame(goFrameW);
                Vector3f boundsCenterS = scene.ToSceneP(originW);

                // translate new object to position in scene
                Frame3f curF = newSO.GetLocalFrame(CoordSpace.SceneCoords);
                curF.Origin += boundsCenterS;
                newSO.SetLocalFrame(curF, CoordSpace.SceneCoords);

                // apply rotation (around current origin)
                curF = newSO.GetLocalFrame(CoordSpace.SceneCoords);
                curF.RotateAround(curF.Origin, goFrameS.Rotation);
                newSO.SetLocalFrame(curF, CoordSpace.SceneCoords);

                // apply local scale
                newSO.SetLocalScale(scale);
            }

            return newSO;
        }


    }




}
