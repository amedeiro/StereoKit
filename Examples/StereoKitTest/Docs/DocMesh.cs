using StereoKit;
using System.Collections.Generic;
using System.Linq;

class DocMesh : ITest
{
    Mesh testMesh = new Mesh();

    bool TestMesh_VertCount()
    {
        Mesh mesh = new Mesh();
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
        Mesh mesh = new Mesh();
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

    bool TestMesh_GetVerts()
    {
        return true;
    }
    
    bool TestMesh_SetGetInds() {
        Mesh mesh = new Mesh();
        //mesh.SetInds(); 
        return true; 
    }

    bool TestMesh_SetGetData() {
        Mesh mesh = new Mesh();
        //mesh.SetData(); //verts and inds
        return true; 
    }

    public void Initialize()
    {
        Tests.Test(TestMesh_VertCount);
        Tests.Test(TestMesh_IndCount);
        Tests.Test(TestMesh_SetGetInds);
        Tests.Test(TestMesh_SetGetData);
    }

    public void Shutdown(){}

    public void Step()
    {
        //Not sure if this stuff is needed yet
        testMesh.Draw(Material.Default, Matrix.Identity);
        Tests.Screenshot("Tests/MeshSubsets.jpg", 600, 600, new Vec3(0, 0.5f, -2), new Vec3(0, 0.5f, 0));
    }
}