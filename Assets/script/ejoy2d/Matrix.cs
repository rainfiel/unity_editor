using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class Matrix {

    public static readonly int[] IdentityMatrix2x3 = { 1024, 0, 0, 1024, 0, 0 };

    public static bool IsIdentityMatrix2x3(int[] mat)
    {
        if (mat.Length != 6) return false;
        for (int i=0; i < 6; i++)
        {
            if (mat[i] != IdentityMatrix2x3[i])
                return false;
        }
        return true;
    }

    public static bool IsMatrix2x3(int[] mat)
    {
        return mat != null && mat.Length == 6;
    }

    public static void MatrixMul(int[] m1, int[] m2, ref int[] m)
    {
        m[0] = (m1[0] * m2[0] + m1[1] * m2[2]) / 1024;
        m[1] = (m1[0] * m2[1] + m1[1] * m2[3]) / 1024;
        m[2] = (m1[2] * m2[0] + m1[3] * m2[2]) / 1024;
        m[3] = (m1[2] * m2[1] + m1[3] * m2[3]) / 1024;
        m[4] = (m1[4] * m2[0] + m1[5] * m2[2]) / 1024 + m2[4];
        m[5] = (m1[4] * m2[1] + m1[5] * m2[3]) / 1024 + m2[5];
    }

    public static Matrix4x4 ToMatrix4x4(int[] mat23, float z=0)
    {
        Matrix4x4 mat = new Matrix4x4();
        mat.SetColumn(0, new Vector4(mat23[0] / 1024.0f, mat23[2] / 1024.0f, 0, 0));
        mat.SetColumn(1, new Vector4(mat23[1] / 1024.0f, mat23[3] / 1024.0f, 0, 0));
        mat.SetColumn(2, new Vector4(0, 0, 1, 0));
        mat.SetColumn(3, new Vector4(mat23[4] / 16, -mat23[5] / 16.0f, z, 1));

        return mat;
    }

    public static int[] TransformToMatrix2x3(Transform transform)
    {
        var mat = Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);
        return ToMatrix2x3(mat);
    }

    public static int[] ToMatrix2x3(Matrix4x4 mat44)
    {
        int[] mat = new int[6];
        mat[0] = (int)(mat44.m00 * 1024);
        mat[1] = (int)(mat44.m01 * 1024);
        mat[2] = (int)(mat44.m10 * 1024);
        mat[3] = (int)(mat44.m11 * 1024);
        mat[4] = (int)(mat44.m03 * 16);
        mat[5] = -(int)(mat44.m13 * 16);
        return mat;
    }

    /// <summary>
    /// Extract translation from transform matrix.
    /// </summary>
    /// <param name="matrix">Transform matrix. This parameter is passed by reference
    /// to improve performance; no changes will be made to it.</param>
    /// <returns>
    /// Translation offset.
    /// </returns>
    public static Vector3 ExtractTranslationFromMatrix(ref Matrix4x4 matrix)
    {
        Vector3 translate;
        translate.x = matrix.m03;
        translate.y = matrix.m13;
        translate.z = matrix.m23;
        return translate;
    }

    /// <summary>
    /// Extract rotation quaternion from transform matrix.
    /// </summary>
    /// <param name="matrix">Transform matrix. This parameter is passed by reference
    /// to improve performance; no changes will be made to it.</param>
    /// <returns>
    /// Quaternion representation of rotation transform.
    /// </returns>
    public static Quaternion ExtractRotationFromMatrix(ref Matrix4x4 matrix)
    {
        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;

        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;

        return Quaternion.LookRotation(forward, upwards);
    }

    /// <summary>
    /// Extract scale from transform matrix.
    /// </summary>
    /// <param name="matrix">Transform matrix. This parameter is passed by reference
    /// to improve performance; no changes will be made to it.</param>
    /// <returns>
    /// Scale vector.
    /// </returns>
    public static Vector3 ExtractScaleFromMatrix(ref Matrix4x4 matrix)
    {
        Vector3 scale;
        scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
        scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
        scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
        return scale;
    }

    /// <summary>
    /// Extract position, rotation and scale from TRS matrix.
    /// </summary>
    /// <param name="matrix">Transform matrix. This parameter is passed by reference
    /// to improve performance; no changes will be made to it.</param>
    /// <param name="localPosition">Output position.</param>
    /// <param name="localRotation">Output rotation.</param>
    /// <param name="localScale">Output scale.</param>
    public static void DecomposeMatrix(ref Matrix4x4 matrix, out Vector3 localPosition, out Quaternion localRotation, out Vector3 localScale)
    {
        localPosition = ExtractTranslationFromMatrix(ref matrix);
        localRotation = ExtractRotationFromMatrix(ref matrix);
        localScale = ExtractScaleFromMatrix(ref matrix);
    }

    /// <summary>
    /// Set transform component from TRS matrix.
    /// </summary>
    /// <param name="transform">Transform component.</param>
    /// <param name="matrix">Transform matrix. This parameter is passed by reference
    /// to improve performance; no changes will be made to it.</param>
    public static void SetTransformFromMatrix(Transform transform, ref Matrix4x4 matrix)
    {
        transform.localPosition = ExtractTranslationFromMatrix(ref matrix);
        transform.localRotation = ExtractRotationFromMatrix(ref matrix);
        transform.localScale = ExtractScaleFromMatrix(ref matrix);
    }


    // EXTRAS!

    /// <summary>
    /// Identity quaternion.
    /// </summary>
    /// <remarks>
    /// <para>It is faster to access this variation than <c>Quaternion.identity</c>.</para>
    /// </remarks>
    public static readonly Quaternion IdentityQuaternion = Quaternion.identity;
    /// <summary>
    /// Identity matrix.
    /// </summary>
    /// <remarks>
    /// <para>It is faster to access this variation than <c>Matrix4x4.identity</c>.</para>
    /// </remarks>
    public static readonly Matrix4x4 IdentityMatrix = Matrix4x4.identity;
    
    /// <summary>
    /// Get translation matrix.
    /// </summary>
    /// <param name="offset">Translation offset.</param>
    /// <returns>
    /// The translation transform matrix.
    /// </returns>
    public static Matrix4x4 TranslationMatrix(Vector3 offset)
    {
        Matrix4x4 matrix = IdentityMatrix;
        matrix.m03 = offset.x;
        matrix.m13 = offset.y;
        matrix.m23 = offset.z;
        return matrix;
    }
}
