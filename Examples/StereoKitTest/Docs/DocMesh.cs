using StereoKit;
using System;
using System.Collections.Generic;
using System.Linq;

class DocMesh : ITest
{
    Mesh mesh = new Mesh();

    bool TestMesh_VertCount()
    {
        mesh = new Mesh();
        if (mesh.VertCount != 0)
        {
            Log.Err("Vert Count is not 0 in new Mesh");
            return false;
        }

        Vertex[] verts_in = new Vertex[10];
        for (int i = 0; i < 10; i++)
        {
            verts_in[i] = new Vertex(new Vec3(i,i,i), new Vec3(i,i,i), new Vec2(i,i), new Color32(0xa, 0xb, 0xc, 0xd));
            mesh.SetVerts(verts_in, false);

            if (mesh.VertCount != i+1)
            {
                Log.Err("Vert count is " + mesh.VertCount + " | Expected vert count: " + (i+1));
                return false;
            }
        }

        return true;
    }

    bool TestMesh_IndCount()
    {
        mesh = new Mesh();
        if (mesh.IndCount != 0)
        {
            return false;
        }

        for (int i = 0; i < 10; i++)
        {
            uint[] inds_in = new uint[3];
            inds_in[0] = (uint)i;
            mesh.SetInds(inds_in);

            if (mesh.VertCount != i + 1)
            {
                return false;
            }
        }

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
        //Run Tests
        //Tests.Test(TestMesh_VertCount);
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