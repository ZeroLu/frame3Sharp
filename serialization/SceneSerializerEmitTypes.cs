﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using g3;

using System.Runtime.InteropServices;


namespace f3
{
    // extension methods to SceneSerializer for emitting built-in types
    public static class SceneSerializerEmitTypesExt
    {


        public static void Emit(this SceneSerializer s, IOutputStream o, CylinderSO so)
        {
            o.AddAttribute(IOStrings.ASOType, IOStrings.TypeCylinder);
            o.AddAttribute(IOStrings.ASOName, so.Name);
            s.EmitTransform(o, so);
            o.AddAttribute(IOStrings.ARadius, so.Radius);
            o.AddAttribute(IOStrings.AHeight, so.Height);
            Frame3f f = so.GetLocalFrame(CoordSpace.ObjectCoords);
            o.AddAttribute(IOStrings.AStartPoint, (f.Origin - 0.5f * so.ScaledHeight * f.Y));
            o.AddAttribute(IOStrings.AEndPoint, (f.Origin + 0.5f * so.ScaledHeight * f.Y));
            s.EmitMaterial(o, so.GetAssignedSOMaterial());
        }

        public static void Emit(this SceneSerializer s, IOutputStream o, BoxSO so)
        {
            o.AddAttribute(IOStrings.ASOType, IOStrings.TypeBox);
            o.AddAttribute(IOStrings.ASOName, so.Name);
            s.EmitTransform(o, so);
            o.AddAttribute(IOStrings.AWidth, so.Width);
            o.AddAttribute(IOStrings.AHeight, so.Height);
            o.AddAttribute(IOStrings.ADepth, so.Depth);
            s.EmitMaterial(o, so.GetAssignedSOMaterial());
        }

        public static void Emit(this SceneSerializer s, IOutputStream o, SphereSO so)
        {
            o.AddAttribute(IOStrings.ASOType, IOStrings.TypeSphere);
            o.AddAttribute(IOStrings.ASOName, so.Name);
            s.EmitTransform(o, so);
            o.AddAttribute(IOStrings.ARadius, so.Radius);
            s.EmitMaterial(o, so.GetAssignedSOMaterial());
        }

        public static void Emit(this SceneSerializer s, IOutputStream o, PivotSO so)
        {
            o.AddAttribute(IOStrings.ASOType, IOStrings.TypePivot);
            o.AddAttribute(IOStrings.ASOName, so.Name);
            s.EmitTransform(o, so);
            s.EmitMaterial(o, so.GetAssignedSOMaterial());
        }


        public static void Emit(this SceneSerializer s, IOutputStream o, MeshSO so)
        {
            o.AddAttribute(IOStrings.ASOType, IOStrings.TypeMesh);
            o.AddAttribute(IOStrings.ASOName, so.Name);
            s.EmitTransform(o, so);
            SimpleMesh m = so.GetSimpleMesh(true);
            s.EmitMeshBinary(m, o);
        }


        public static void Emit(this SceneSerializer s, IOutputStream o, MeshReferenceSO so)
        {
            o.AddAttribute(IOStrings.ASOType, IOStrings.TypeMeshReference);
            o.AddAttribute(IOStrings.ASOName, so.Name);
            s.EmitTransform(o, so);
            // [TODO] be smarter about paths
            o.AddAttribute(IOStrings.AReferencePath, so.MeshReferencePath);

            StringBuilder rel_path = new StringBuilder(260); // MAX_PATH
            if ( PathRelativePathTo(rel_path,
                Path.GetDirectoryName(s.TargetFilePath), FILE_ATTRIBUTE_DIRECTORY, 
                    so.MeshReferencePath, FILE_ATTRIBUTE_NORMAL) == 1) {
                o.AddAttribute(IOStrings.ARelReferencePath, rel_path.ToString());
            }
        }

        // PathRelativePathTo function is only on windows?
        [DllImport("shlwapi.dll", SetLastError = true)]
        private static extern int PathRelativePathTo(StringBuilder pszPath,
            string pszFrom, int dwAttrFrom, string pszTo, int dwAttrTo);
        private const int FILE_ATTRIBUTE_DIRECTORY = 0x10;
        private const int FILE_ATTRIBUTE_NORMAL = 0x80;


        public static void Emit(this SceneSerializer s, IOutputStream o, TransformableSO so)
        {
            o.AddAttribute(IOStrings.ASOType, IOStrings.TypeUnknown);
            o.AddAttribute(IOStrings.ASOName, so.Name);
            s.EmitTransform(o, so);
        }



        public static void Emit(this SceneSerializer s, IOutputStream o, PolyCurveSO so)
        {
            o.AddAttribute(IOStrings.ASOType, IOStrings.TypePolyCurve);
            o.AddAttribute(IOStrings.ASOName, so.Name);
            s.EmitTransform(o, so);
            s.EmitMaterial(o, so.GetAssignedSOMaterial());
            o.AddAttribute(IOStrings.APolyCurve3, so.Curve.Vertices);
            o.AddAttribute(IOStrings.APolyCurveClosed, so.Curve.Closed);
        }

        public static void Emit(this SceneSerializer s, IOutputStream o, PolyTubeSO so)
        {
            o.AddAttribute(IOStrings.ASOType, IOStrings.TypePolyTube);
            o.AddAttribute(IOStrings.ASOName, so.Name);
            s.EmitTransform(o, so);
            s.EmitMaterial(o, so.GetAssignedSOMaterial());
            o.AddAttribute(IOStrings.APolyCurve3, so.Curve.Vertices );
            o.AddAttribute(IOStrings.APolyCurveClosed, so.Curve.Closed );
            o.AddAttribute(IOStrings.APolygon2, so.Polygon.Vertices);
        }







        public static void EmitTransform(this SceneSerializer s, IOutputStream o, TransformableSO so)
        {
            o.BeginStruct(IOStrings.TransformStruct);
            Frame3f f = so.GetLocalFrame(CoordSpace.ObjectCoords);
            o.AddAttribute(IOStrings.APosition, f.Origin );
            o.AddAttribute(IOStrings.AOrientation, f.Rotation );
            o.AddAttribute(IOStrings.AScale, so.RootGameObject.transform.localScale);
            o.EndStruct();
        }

        public static void EmitMaterial(this SceneSerializer s, IOutputStream o, SOMaterial mat)
        {
            o.BeginStruct(IOStrings.MaterialStruct);
            if (mat.Type == SOMaterial.MaterialType.StandardRGBColor) {
                o.AddAttribute(IOStrings.AMaterialType, IOStrings.AMaterialType_Standard);
                o.AddAttribute(IOStrings.AMaterialName, mat.Name);
                o.AddAttribute(IOStrings.AMaterialRGBColor, mat.RGBColor);
            } else if ( mat.Type == SOMaterial.MaterialType.TransparentRGBColor) {
                o.AddAttribute(IOStrings.AMaterialType, IOStrings.AMaterialType_Transparent);
                o.AddAttribute(IOStrings.AMaterialName, mat.Name);
                o.AddAttribute(IOStrings.AMaterialRGBColor, mat.RGBColor);
            }
            o.EndStruct();
        }



        public static void EmitMeshAscii(this SceneSerializer s, SimpleMesh m, IOutputStream o)
        {
            o.BeginStruct(IOStrings.AsciiMeshStruct);
            o.AddAttribute(IOStrings.AMeshVertices3, m.VerticesItr());
            if (m.HasVertexNormals)
                o.AddAttribute(IOStrings.AMeshNormals3, m.NormalsItr());
            if (m.HasVertexColors)
                o.AddAttribute(IOStrings.AMeshColors3, m.ColorsItr());
            if (m.HasVertexUVs)
                o.AddAttribute(IOStrings.AMeshUVs2, m.UVsItr());
            o.AddAttribute(IOStrings.AMeshTriangles, m.TrianglesItr());
            o.EndStruct();
        }


        public static void EmitMeshBinary(this SceneSerializer s, SimpleMesh m, IOutputStream o)
        {
            // binary version - uuencoded byte buffers
            //    - storing doubles uses roughly same mem as string, but string is only 8 digits precision
            //    - storing floats saves roughly 50%
            //    - storing triangles is worse until vertex count > 9999
            //          - could store as byte or short in those cases...
            o.BeginStruct(IOStrings.BinaryMeshStruct);
            o.AddAttribute(IOStrings.AMeshVertices3Binary, m.Vertices.GetBytes());
            if (m.HasVertexNormals)
                o.AddAttribute(IOStrings.AMeshNormals3Binary, m.Normals.GetBytes());
            if (m.HasVertexColors)
                o.AddAttribute(IOStrings.AMeshColors3Binary, m.Colors.GetBytes());
            if (m.HasVertexUVs)
                o.AddAttribute(IOStrings.AMeshUVs2Binary, m.UVs.GetBytes());
            o.AddAttribute(IOStrings.AMeshTrianglesBinary, m.Triangles.GetBytes());
            o.EndStruct();
        }


        public static void EmitKeyframes(this SceneSerializer s, KeyframeSequence seq, IOutputStream o)
        {
            o.BeginStruct(IOStrings.KeyframeListStruct);
            o.AddAttribute(IOStrings.ATimeRange, (Vector2f)seq.ValidRange);

            int i = 0;
            foreach ( Keyframe k  in seq ) {
                o.BeginStruct(IOStrings.KeyframeStruct, i.ToString());
                i++;
                o.AddAttribute(IOStrings.ATime, (float)k.Time, true );
                o.AddAttribute(IOStrings.APosition, k.Frame.Origin, true );
                o.AddAttribute(IOStrings.AOrientation, k.Frame.Rotation, true );
                o.EndStruct();
            }

            o.EndStruct();
        }

    }



}
