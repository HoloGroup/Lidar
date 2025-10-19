using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
public class VRTeleportation_VRModelSerializer
{
    public Material MaterialForDeserialize;

    private class MeshFilterData
    {
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 Scale;

        public Vector3[] Verts;
        public int[] Tris;
        public Vector3[] Normals;
        public Vector2[] UV;
        public int TextureId;
        public byte[] Texture = new byte[0];

        public Texture2D Texture2D
        {
            get
            {
                if (Texture.Length == 0)
                    return null;
                else
                {
                    var tex = new Texture2D(1, 1);
                    var res = tex.LoadImage(Texture);
                    tex.Apply();
                    Debug.Log($"Is readable: {tex.isReadable}");
                    return tex;
                }
            }
        }

        public List<byte> Serialize()
        {
            List<byte> data = new List<byte>();

            data.AddRange(GetBytes(LocalPosition));
            data.AddRange(GetBytes(LocalRotation));
            data.AddRange(GetBytes(Scale));

            data.AddRange(GetBytes(Verts));
            data.AddRange(GetBytes(Tris));
            data.AddRange(GetBytes(Normals));
            data.AddRange(GetBytes(UV));
            //data.AddRange(BitConverter.GetBytes(TextureId));
            data.AddRange(BitConverter.GetBytes(Texture.Length));

            if (Texture.Length > 0)
                data.AddRange(Texture);

            return data;
        }

        public int Deserialize(byte[] data, int offset)
        {
            offset = GetVector3(data, offset, out LocalPosition);
            offset = GetQuaternion(data, offset, out LocalRotation);
            offset = GetVector3(data, offset, out Scale);

            offset = GetVector3Array(data, offset, out Verts);
            offset = GetIntArray(data, offset, out Tris);
            offset = GetVector3Array(data, offset, out Normals);
            offset = GetVector2Array(data, offset, out UV);
            //TextureId = BitConverter.ToInt32(data, offset);
            //offset += 4;
            var texLenght = BitConverter.ToInt32(data, offset);
            offset += 4;

            if (texLenght > 0)
            {
                Texture = new byte[texLenght];
                Buffer.BlockCopy(data, offset, Texture, 0, texLenght);


                offset += texLenght;
            }

            return offset;
        }

        private List<byte> GetBytes(Vector3 input)
        {
            List<byte> data = new List<byte>();

            data.AddRange(BitConverter.GetBytes(input.x));
            data.AddRange(BitConverter.GetBytes(input.y));
            data.AddRange(BitConverter.GetBytes(input.z));

            return data;
        }

        private List<byte> GetBytes(Vector2 input)
        {
            List<byte> data = new List<byte>();

            data.AddRange(BitConverter.GetBytes(input.x));
            data.AddRange(BitConverter.GetBytes(input.y));

            return data;
        }

        private List<byte> GetBytes(Quaternion input)
        {
            var inp = input.eulerAngles;

            List<byte> data = new List<byte>();


            data.AddRange(BitConverter.GetBytes(inp.x));
            data.AddRange(BitConverter.GetBytes(inp.y));
            data.AddRange(BitConverter.GetBytes(inp.z));

            return data;
        }

        private List<byte> GetBytes(Vector3[] input)
        {
            List<byte> data = new List<byte>();


            data.AddRange(BitConverter.GetBytes(input.Length));
            foreach (var part in input)
            {
                data.AddRange(GetBytes(part));
            }

            return data;
        }

        private List<byte> GetBytes(int[] input)
        {
            List<byte> data = new List<byte>();


            data.AddRange(BitConverter.GetBytes(input.Length));
            foreach (var part in input)
            {
                data.AddRange(BitConverter.GetBytes(part));
            }

            return data;
        }

        private List<byte> GetBytes(Vector2[] input)
        {
            List<byte> data = new List<byte>();


            data.AddRange(BitConverter.GetBytes(input.Length));
            foreach (var part in input)
            {
                data.AddRange(GetBytes(part));
            }

            return data;
        }



        private int GetVector3(byte[] data, int offset, out Vector3 output)
        {
            var x = BitConverter.ToSingle(data, offset);
            offset += 4;

            var y = BitConverter.ToSingle(data, offset);
            offset += 4;

            var z = BitConverter.ToSingle(data, offset);
            offset += 4;

            output = new Vector3(x, y, z);

            return offset;
        }


        private int GetVector2(byte[] data, int offset, out Vector2 output)
        {
            var x = BitConverter.ToSingle(data, offset);
            offset += 4;

            var y = BitConverter.ToSingle(data, offset);
            offset += 4;

            output = new Vector2(x, y);

            return offset;
        }

        private int GetQuaternion(byte[] data, int offset, out Quaternion output)
        {
            var x = BitConverter.ToSingle(data, offset);
            offset += 4;

            var y = BitConverter.ToSingle(data, offset);
            offset += 4;

            var z = BitConverter.ToSingle(data, offset);
            offset += 4;

            output = Quaternion.Euler(x, y, z);

            return offset;
        }


        private int GetVector3Array(byte[] data, int offset, out Vector3[] output)
        {
            var count = BitConverter.ToInt32(data, offset);
            offset += 4;

            List<Vector3> result = new List<Vector3>(count);

            for (int i = 0; i < count; ++i)
            {
                Vector3 vector;
                offset = GetVector3(data, offset, out vector);
                result.Add(vector);
            }

            output = result.ToArray();

            return offset;
        }

        private int GetVector2Array(byte[] data, int offset, out Vector2[] output)
        {
            var count = BitConverter.ToInt32(data, offset);
            offset += 4;

            List<Vector2> result = new List<Vector2>(count);

            for (int i = 0; i < count; ++i)
            {
                Vector2 vector;
                offset = GetVector2(data, offset, out vector);
                result.Add(vector);
            }

            output = result.ToArray();

            return offset;
        }

        private int GetIntArray(byte[] data, int offset, out int[] output)
        {
            var count = BitConverter.ToInt32(data, offset);
            offset += 4;

            int[] result = new int[count];

            for (int i = 0; i < count; ++i)
            {
                result[i] = BitConverter.ToInt32(data, offset);
                offset += 4;
            }

            output = result;

            return offset;
        }
    }


    //public async UniTask<byte[]> Serialize(GameObject model, Action<int> onProgressChanged = null)
    //{
    //    onProgressChanged?.Invoke(0);

    //    var allMeshFilters = model.GetComponentsInChildren<MeshFilter>();
    //    List<MeshFilterData> mfDatas = new List<MeshFilterData>(allMeshFilters.Length);
    //    var steps = allMeshFilters.Length;
    //    var tempStep = 0;

    //    int countProcessed = 0;
    //    foreach (var mf in allMeshFilters)
    //    {
    //        if (countProcessed > 50)
    //        {
    //            countProcessed = 0;
    //            await UniTask.Yield();
    //        }

    //        var mfData = new MeshFilterData();
    //        var mfTransform = mf.transform;
    //        mfData.LocalPosition = mfTransform.localPosition;
    //        mfData.LocalRotation = mfTransform.localRotation;
    //        mfData.Scale = mfTransform.localScale;

    //        mfData.Verts = mf.sharedMesh.vertices;
    //        mfData.Tris = mf.sharedMesh.triangles;
    //        mfData.Normals = mf.sharedMesh.normals;
    //        mfData.UV = mf.sharedMesh.uv;

    //        var tex = mf.GetComponent<MeshRenderer>().material.GetTexture("_BaseMap");
    //        if (tex != null)
    //        {
    //            var tex2d = (tex as Texture2D);
    //            mfData.Texture = tex2d.EncodeToJPG();
    //        }

    //        mfDatas.Add(mfData);

    //        ++countProcessed;

    //        tempStep++;

    //        onProgressChanged?.Invoke((int)(tempStep * 100f) / steps);
    //    }


    //    var resultData = new List<byte>();

    //    resultData.AddRange(BitConverter.GetBytes(mfDatas.Count));
    //    foreach (var data in mfDatas)
    //    {
    //        resultData.AddRange(data.Serialize());
    //    }

    //    return resultData.ToArray();
    //}

    public GameObject Deserialize(byte[] data, int offset)
    {
        int meshCount = BitConverter.ToInt32(data, offset);
        offset += 4;

        List<MeshFilterData> mfDatas = new List<MeshFilterData>(meshCount);

        for (int i = 0; i < meshCount; ++i)
        {
            var mfData = new MeshFilterData();
            offset = mfData.Deserialize(data, offset);

            mfDatas.Add(mfData);
        }

        var parent = new GameObject("Restored model");
        var parentTransform = parent.transform;

        foreach (var mfData in mfDatas)
        {
            var child = new GameObject();
            child.transform.parent = parentTransform;
            child.transform.localPosition = mfData.LocalPosition;
            child.transform.localRotation = mfData.LocalRotation;
            child.transform.localScale = mfData.Scale;

            var mf = child.AddComponent<MeshFilter>();

            var mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.vertices = mfData.Verts;
            mesh.triangles = mfData.Tris;
            mesh.normals = mfData.Normals;
            mesh.uv = mfData.UV;

            mf.sharedMesh = mesh;

            var mr = child.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialForDeserialize;

            var tex = mfData.Texture2D;
            if (tex != null)
                mr.material.SetTexture("_BaseMap", tex);
        }

        return parent;
    }

    //public async Task<byte[]> Serialize(Dictionary<string, List<MeshSlicerNative.MeshPart>> slisedMeshes, Action<int> onProgressChanged)
    //{
    //    onProgressChanged?.Invoke(0);
    //    List<MeshSlicerNative.MeshPart> allMeshParts = new List<MeshSlicerNative.MeshPart>();
    //    List<MeshSlicerNative.MeshPartTextureInfo> allTextures = new List<MeshSlicerNative.MeshPartTextureInfo>();

    //    foreach (var part in slisedMeshes)
    //    {
    //        if (part.Value.Count > 0)
    //            allMeshParts.AddRange(part.Value);
    //    }

    //    foreach (var part in allMeshParts)
    //    {
    //        if (allTextures.Contains(part.TextureInfo))
    //            continue;

    //        allTextures.Add(part.TextureInfo);
    //    }


    //    Debug.Log($"Mesh count: {allMeshParts.Count}, texture count {allTextures.Count}");
    //    List<byte> resultData = new List<byte>();
    //    List<MeshFilterData> mfDatas = new List<MeshFilterData>(allMeshParts.Count);


    //    var steps = allMeshParts.Count + allTextures.Count;
    //    var tempStep = 0;
    //    int countProcessed = 0;

    //    resultData.AddRange(BitConverter.GetBytes(allTextures.Count));

    //    foreach (var textureInfo in allTextures)
    //    {
    //        if (countProcessed > 10)
    //        {
    //            countProcessed = 0;
    //            await UniTask.Yield();
    //        }

    //        var textureBytes = textureInfo.Texture.EncodeToJPG();

    //        resultData.AddRange(BitConverter.GetBytes(textureInfo.Id));
    //        resultData.AddRange(BitConverter.GetBytes(textureBytes.Length));
    //        resultData.AddRange(textureBytes);



    //        ++countProcessed;
    //        tempStep++;
    //        onProgressChanged?.Invoke((int)(tempStep * 100f) / steps);
    //    }


    //    foreach (var meshPart in allMeshParts)
    //    {
    //        if (countProcessed > 50)
    //        {
    //            countProcessed = 0;
    //            await UniTask.Yield();
    //        }

    //        var mf = meshPart.Filter;

    //        var mfData = new MeshFilterData();
    //        var mfTransform = mf.transform;
    //        mfData.LocalPosition = mfTransform.localPosition;
    //        mfData.LocalRotation = mfTransform.localRotation;
    //        mfData.Scale = mfTransform.localScale;

    //        mfData.Verts = mf.sharedMesh.vertices;
    //        mfData.Tris = mf.sharedMesh.triangles;
    //        mfData.Normals = mf.sharedMesh.normals;
    //        mfData.UV = mf.sharedMesh.uv;
    //        mfData.TextureId = meshPart.TextureInfo.Id;

    //        mfDatas.Add(mfData);

    //        ++countProcessed;

    //        tempStep++;

    //        onProgressChanged?.Invoke((int)(tempStep * 100f) / steps);
    //    }



    //    resultData.AddRange(BitConverter.GetBytes(mfDatas.Count));
    //    foreach (var data in mfDatas)
    //    {
    //        resultData.AddRange(data.Serialize());
    //    }

    //    return resultData.ToArray();
    //}

    //public GameObject Deserialize(byte[] data, int offset)
    //{
    //    Dictionary<int, Texture2D> allTextures = new Dictionary<int, Texture2D>();

    //    int texturesCount = BitConverter.ToInt32(data, offset);
    //    offset += 4;

    //    for (int i = 0; i < texturesCount; ++i)
    //    {
    //        int id = BitConverter.ToInt32(data, offset);
    //        offset += 4;
    //        int lenght = BitConverter.ToInt32(data, offset);
    //        offset += 4;

    //        var texture = new byte[lenght];
    //        Buffer.BlockCopy(data, offset, texture, 0, lenght);
    //        offset += lenght;

    //        var tex = new Texture2D(1, 1);
    //        tex.LoadImage(texture);
    //        tex.Apply();

    //        allTextures.Add(id, tex);
    //    }


    //    int meshCount = BitConverter.ToInt32(data, offset);
    //    offset += 4;

    //    List<MeshFilterData> mfDatas = new List<MeshFilterData>(meshCount);

    //    for (int i = 0; i < meshCount; ++i)
    //    {
    //        var mfData = new MeshFilterData();
    //        offset = mfData.Deserialize(data, offset);

    //        mfDatas.Add(mfData);
    //    }

    //    var parent = new GameObject("Restored model");
    //    var parentTransform = parent.transform;

    //    foreach (var mfData in mfDatas)
    //    {
    //        var child = new GameObject();
    //        child.transform.parent = parentTransform;
    //        child.transform.localPosition = mfData.LocalPosition;
    //        child.transform.localRotation = mfData.LocalRotation;
    //        child.transform.localScale = mfData.Scale;

    //        var mf = child.AddComponent<MeshFilter>();

    //        var mesh = new Mesh();
    //        mesh.vertices = mfData.Verts;
    //        mesh.triangles = mfData.Tris;
    //        mesh.normals = mfData.Normals;
    //        mesh.uv = mfData.UV;

    //        mf.sharedMesh = mesh;

    //        var mr = child.AddComponent<MeshRenderer>();
    //        mr.sharedMaterial = MaterialForDeserialize;

    //        var tex = allTextures[mfData.TextureId];
    //        if (tex != null)
    //            mr.material.SetTexture("_BaseMap", tex);
    //    }

    //    return parent;
    //}
}



