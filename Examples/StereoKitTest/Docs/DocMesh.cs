using StereoKit;
using System;
using System.Collections.Generic;
using System.Linq;

class DocMesh : ITest
{
    Mesh testMesh = new Mesh();

    bool TestMesh_SetInds()
    {
        Mesh mesh = new Mesh();
        uint[] inds_in = new uint[3];
        inds_in[0] = 0;
        inds_in[1] = 1;
        inds_in[2] = 2;
        mesh.SetInds(inds_in);
        if (mesh.IndCount != 3)
        {
            return false;
        }
        uint[] inds_out = mesh.GetInds();
        foreach (uint i in mesh.GetInds())
        {
            if (inds_in[i] != inds_out[i])
            {
                return false;
            }
        }
        Log.Info("SetInds test method TestMesh_SetInds: Pass!");
        return true;
    }
    bool TestMesh_SetIndsNull()
    {
        Mesh mesh = new Mesh();
        try
        {
            mesh.SetInds(null);
        }
        catch(System.NullReferenceException e)
        {
            Log.Info("SetInds test method TestMesh_SetIndsNull: Pass!");
            return true;
        }
        return false;
    }

    bool TestMesh_GenerateSphereValidDiameter()
    {
        Mesh mesh = Mesh.GenerateSphere(10,4);
        if (mesh.VertCount != 216)
        {
            Log.Err("Sphere mesh has " + mesh.VertCount + " vertices, expected 216");
            return false;
        }
        Log.Info("Sphere mesh test method TestMesh_GenerateSphereValidDiameter: Pass!");
        return true;
    }
    bool TestMesh_GenerateSphereNegatives()
    {
        Mesh mesh = Mesh.GenerateSphere(-1, -4);
        if (mesh.VertCount != 24)
        {
            Log.Err("Sphere mesh has " + mesh.VertCount + " vertices, expected 24");
            return false;
        }
        Log.Info("Sphere mesh test method TestMesh_GenerateSphereNegatives: Pass!");
        return true;
    }
    bool TestMesh_GenerateSphereValidFloatDiameter()
    {
        try
        {
            Mesh mesh = Mesh.GenerateSphere(10.5f, 40);
        }catch(System.ArgumentException e)
        {
            return false;
        }
        Log.Info("Sphere mesh test method TestMesh_GenerateSphereValidFloatDiameter: Pass!");
        return true;
    }
    public void Initialize()
    {
        Tests.Test(TestMesh_SetInds);
        Tests.Test(TestMesh_SetIndsNull);
        Tests.Test(TestMesh_GenerateSphereValidDiameter);
        Tests.Test(TestMesh_GenerateSphereNegatives);
        Tests.Test(TestMesh_GenerateSphereValidFloatDiameter);
    }

    public void Shutdown(){}

    public void Step()
    {
        //Not sure if this stuff is needed yet
        testMesh.Draw(Material.Default, Matrix.Identity);
        Tests.Screenshot("Tests/MeshSubsets.jpg", 600, 600, new Vec3(0, 0.5f, -2), new Vec3(0, 0.5f, 0));
    }
}