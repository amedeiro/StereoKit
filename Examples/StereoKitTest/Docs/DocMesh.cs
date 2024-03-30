using StereoKit;
using System;
using System.Collections.Generic;
using System.Linq;

class DocMesh : ITest
{
    Mesh mesh = new Mesh();

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
        Log.Info("TestMesh_SetInds: Pass!");
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
            Log.Info("TestMesh_SetIndsNull: Pass!");
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
        Log.Info("TestMesh_GenerateSphereValidDiameter: Pass!");
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
        Log.Info("TestMesh_GenerateSphereNegatives: Pass!");
        return true;
    }
    bool TestMesh_GenerateSphereValidFloatDiameter()
    {
        try
        {
            Mesh mesh = Mesh.GenerateSphere(10.5f, 40);
        }
        catch (System.ArgumentException e)
        {
            return false;
        }
        Log.Info("TestMesh_GenerateSphereValidFloatDiameter: Pass!");
        return true;
    }

    bool TestMesh_Intersect_True()
    {
        mesh = new Mesh();
        Ray outRay;
        Vertex[] verts_in = new Vertex[3];
        verts_in[0] = new Vertex(new Vec3(1,0,0), new Vec3(0, 1, 0));
        verts_in[1] = new Vertex(new Vec3(0,1,0), new Vec3(0, 1, 0));
        verts_in[2] = new Vertex(new Vec3(0,0,1), new Vec3(0, 1, 0));
        mesh.SetVerts(verts_in);

        uint[] inds = new uint[] { 2, 1, 0 };
        mesh.SetInds(inds);

        Ray ray = new Ray(new Vec3(0, 0, 0), new Vec3(1, 1, 1));
        bool intersect = mesh.Intersect(ray, out outRay);

        if (intersect && outRay.position.Equals(new Vec3(0.33333334f)) && outRay.direction.Equals(new Vec3(-0.57735026f)))
        {
            Log.Info("TestMesh_Intersect_True: Pass!");
            return true;
        } else if (!intersect)
        {
            Log.Err("Intersection Expected, but did not happen:");
            Log.Err("Verts[0]: " + verts_in[0].pos.ToString() + " | " + verts_in[0].norm.ToString());
            Log.Err("Verts[1]: " + verts_in[1].pos.ToString() + " | " + verts_in[1].norm.ToString());
            Log.Err("Verts[2]: " + verts_in[2].pos.ToString() + " | " + verts_in[2].norm.ToString());
            Log.Err("Inds: [" + inds[0] + "," + inds[1] + "," + inds[2] + "]");
            Log.Err("Ray: " + ray);
        } else if (!outRay.position.Equals(new Vec3(0.33333334f)))
        {
            Log.Err("Intersection point position expected to be [0.33,0.33,0.33] but was " +  outRay.position.ToString());
        } else if (!outRay.direction.Equals(new Vec3(-0.57735026f)))
        {
            Log.Err("Intersection point position expected to be [-0.58,-0.58,-0.58] but was " + outRay.direction.ToString());
        }

        return false;
    }

    bool TestMesh_Intersect_False()
    {
        mesh = new Mesh();
        Ray outRay;
        Vertex[] verts_in = new Vertex[3];
        verts_in[0] = new Vertex(new Vec3(1), new Vec3(0, 1, 0));
        verts_in[1] = new Vertex(new Vec3(-1), new Vec3(0, 1, 0));
        verts_in[2] = new Vertex(new Vec3(0), new Vec3(0, 1, 0));
        mesh.SetVerts(verts_in);

        uint[] inds = new uint[] { 2, 1, 0 };
        mesh.SetInds(inds);

        // Not expected to intersect
        Ray ray = new Ray(new Vec3(0, 0, 0), new Vec3(1, 1, 1));
        bool intersect = mesh.Intersect(ray, out outRay);

        //intersect should be false and the out ray is expected to be all zeros
        if (!intersect && outRay.position.Equals(new Vec3(0f)) && outRay.direction.Equals(new Vec3(0f)))
        {
            Log.Info("TestMesh_Intersect_False: Pass!");
            return true;
        } else if (intersect)
        {
            Log.Err("Intersection Was Not Expected, but happened:");
            Log.Err("Verts[0]: " + verts_in[0].ToString());
            Log.Err("Verts[1]: " + verts_in[1].ToString());
            Log.Err("Verts[2]: " + verts_in[2].ToString());
            Log.Err("Inds: " + inds.ToString());
            Log.Err("Ray: " + ray.ToString());
        }
        else if (!outRay.position.Equals(new Vec3(0)))
        {
            Log.Err("No intersection should result in position [0,0,0], but was: " + outRay.position.ToString());
        }
        else if (!outRay.direction.Equals(new Vec3(0)))
        {
            Log.Err("No intersection should result in direction [0,0,0], but was: " + outRay.direction.ToString());
        }
        return false;
    }

    bool TestMesh_Intersect_True_FullOutRay()
    {
        mesh = new Mesh();
        Ray outRay = new Ray(new Vec3(0.5f, 0.5f, 0.5f), new Vec3(0.5f, 0.5f, 0.5f));
        Vertex[] verts_in = new Vertex[3];
        verts_in[0] = new Vertex(new Vec3(1, 0, 0), new Vec3(0, 1, 0));
        verts_in[1] = new Vertex(new Vec3(0, 1, 0), new Vec3(0, 1, 0));
        verts_in[2] = new Vertex(new Vec3(0, 0, 1), new Vec3(0, 1, 0));
        mesh.SetVerts(verts_in);

        uint[] inds = new uint[] { 2, 1, 0 };
        mesh.SetInds(inds);

        Ray ray = new Ray(new Vec3(0, 0, 0), new Vec3(1, 1, 1));
        bool intersect = mesh.Intersect(ray, out outRay);

        if (intersect && outRay.position.Equals(new Vec3(0.33333334f)) && outRay.direction.Equals(new Vec3(-0.57735026f)))
        {
            Log.Info("TestMesh_Intersect_True: Pass!");
            return true;
        }
        else if (!intersect)
        {
            Log.Err("Intersection Expected, but did not happen:");
            Log.Err("Verts[0]: " + verts_in[0].ToString());
            Log.Err("Verts[1]: " + verts_in[1].ToString());
            Log.Err("Verts[2]: " + verts_in[2].ToString());
            Log.Err("Inds: " + inds.ToString());
            Log.Err("Ray: " + ray.ToString());
        }
        else if (!outRay.position.Equals(new Vec3(0.33333334f)))
        {
            Log.Err("Intersection point position expected to be [0.33,0.33,0.33] but was " + outRay.position.ToString());
        }
        else if (!outRay.direction.Equals(new Vec3(-0.57735026f)))
        {
            Log.Err("Intersection point position expected to be [-0.58,-0.58,-0.58] but was " + outRay.direction.ToString());
        }

        return false;
    }

    bool TestMesh_Intersect_False_FullOutRay()
    {
        mesh = new Mesh();
        Ray outRay = new Ray(new Vec3(0.5f, 0.5f, 0.5f), new Vec3(0.5f, 0.5f, 0.5f));
        Vertex[] verts_in = new Vertex[3];
        verts_in[0] = new Vertex(new Vec3(1), new Vec3(0, 1, 0));
        verts_in[1] = new Vertex(new Vec3(-1), new Vec3(0, 1, 0));
        verts_in[2] = new Vertex(new Vec3(0), new Vec3(0, 1, 0));
        mesh.SetVerts(verts_in);

        uint[] inds = new uint[] { 2, 1, 0 };
        mesh.SetInds(inds);

        // Not expected to intersect
        Ray ray = new Ray(new Vec3(0, 0, 0), new Vec3(1, 1, 1));
        bool intersect = mesh.Intersect(ray, out outRay);

        //intersect should be false and the out ray is expected to be all zeros
        if (!intersect && outRay.position.Equals(new Vec3(0f)) && outRay.direction.Equals(new Vec3(0f)))
        {
            Log.Info("TestMesh_Intersect_False: Pass!");
            return true;
        }
        else if (intersect)
        {
            Log.Err("Intersection Was Not Expected, but happened:");
            Log.Err("Verts[0]: " + verts_in[0].ToString());
            Log.Err("Verts[1]: " + verts_in[1].ToString());
            Log.Err("Verts[2]: " + verts_in[2].ToString());
            Log.Err("Inds: " + inds.ToString());
            Log.Err("Ray: " + ray.ToString());
        }
        else if (!outRay.position.Equals(new Vec3(0)))
        {
            Log.Err("No intersection should result in position [0,0,0], but was: " + outRay.position.ToString());
        }
        else if (!outRay.direction.Equals(new Vec3(0)))
        {
            Log.Err("No intersection should result in direction [0,0,0], but was: " + outRay.direction.ToString());
        }
        return false;
    }

    bool TestMesh_GenerateCircleValidDiameter()
    {
        Mesh mesh = Mesh.GenerateCircle(5.0f, 8, true);
        if (mesh.VertCount == 16)
        {
            Log.Info("TestMesh_GenerateCircleValidDiameter: Pass!");
            return true;
        }
        Log.Err("Circle Mesh has " + mesh.VertCount + " vertices, expected 16");
        return false;   
    }

    bool TestMesh_GenerateCircleNegatives()
    {
        Mesh mesh = Mesh.GenerateCircle(-5.0f, -8, true);
        if (mesh.VertCount == 6)
        {
            Log.Info("TestMesh_GenerateCircleNegatives: Pass!");
            return true;
        }
        Log.Err("Circle Mesh has " + mesh.VertCount + " vertices, expected 6");
        return false;
    }

    bool TestMesh_GenerateCircle_CustomSpokes()
    {
        var mesh = new Mesh();
        var meshCustomSpokes = Mesh.GenerateCircle(10f, 32);

        if (meshCustomSpokes.VertCount == 32)
        {
            Log.Info("TestMesh_GenerateCircle_CustomSpokes: Pass!");
            return true;
        }
        Log.Err("Vert Count expected to be 32, but was: " + meshCustomSpokes.VertCount);
        return false;
    }

    public void Initialize()
    {
        Tests.Test(TestMesh_SetInds);
        Tests.Test(TestMesh_SetIndsNull);
        Tests.Test(TestMesh_GenerateSphereValidDiameter);
        Tests.Test(TestMesh_GenerateSphereNegatives);
        Tests.Test(TestMesh_GenerateSphereValidFloatDiameter);
        Tests.Test(TestMesh_Intersect_True);
        Tests.Test(TestMesh_Intersect_False);
        Tests.Test(TestMesh_Intersect_True_FullOutRay);
        Tests.Test(TestMesh_Intersect_False_FullOutRay);
        Tests.Test(TestMesh_GenerateCircle_CustomSpokes);
        Tests.Test(TestMesh_GenerateCircleNegatives);
        Tests.Test(TestMesh_GenerateCircleValidDiameter);
    }

    public void Shutdown(){}

    public void Step()
    {
        //Not sure if this stuff is needed yet
        mesh.Draw(Material.Default, Matrix.Identity);
        Tests.Screenshot("Tests/MeshSubsets.jpg", 600, 600, new Vec3(0, 0.5f, -2), new Vec3(0, 0.5f, 0));
    }
}