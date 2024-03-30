using StereoKit;
using StereoKit.Framework;
using System;
using System.Linq;
using System.Reflection.Metadata;

class DocHandMenu : ITest
{
    bool Test_HandMenuRadial_ThreeLayers_MultipleItems()
    {
        HandMenuRadial handMenuRadial;
        try
        {

            handMenuRadial = new HandMenuRadial(
            new HandRadialLayer("Root",
                new HandMenuItem("File", null, null, "File"),
                new HandMenuItem("Search", null, null, "Edit"),
                new HandMenuItem("About", Sprite.FromFile("search.png"), () => Log.Info(SK.VersionName)),
                new HandMenuItem("Cancel", null, null)),
            new HandRadialLayer("File",
                new HandMenuItem("New", null, () => Log.Info("New")),
                new HandMenuItem("Open", null, () => Log.Info("Open")),
                new HandMenuItem("Close", null, () => Log.Info("Close")),
                new HandMenuItem("Back", null, null, HandMenuAction.Back)),
            new HandRadialLayer("Edit",
                new HandMenuItem("Copy", null, () => Log.Info("Copy")),
                new HandMenuItem("Paste", null, () => Log.Info("Paste")),
                new HandMenuItem("Back", null, null, HandMenuAction.Back)));

        } catch (System.NullReferenceException e )
        {
            Log.Err("HandMenuRadial threw a Null Pointer Exception");
            return false;
        }

        if (handMenuRadial.RootLayer.layerName.Equals("Root") &&
            handMenuRadial.RootLayer.items.ElementAt(0).name.Equals("File") &&
            handMenuRadial.RootLayer.items.ElementAt(0).layerName.Equals("File") &&
            handMenuRadial.RootLayer.items.ElementAt(0).action.ToString().Equals("Layer"))
        {
            if (handMenuRadial.RootLayer.FindChild("File") != null)
            {
                if (handMenuRadial.RootLayer.FindChild("Edit") != null)
                {
                    Log.Info("Test_HandMenuRadial_TwoLayers_OneItemEach: Pass!");
                    return true;
                } else
                {
                    Log.Err("HandMenuRadial child layers were incorrectly set");
                    return false;
                }
            } else
            {
                Log.Err("HandMenuRadial child layers were incorrectly set");
                return false;
            }

        } else
        {
            Log.Err("HandMenuRadial root layer was incorrectly set");
            return false;
        }
    }

    bool Test_HandMenuRadial_NullLayers()
    {
        try
        {
            HandMenuRadial handMenuRadial = new HandMenuRadial(null);
            if (handMenuRadial.RootLayer.layerName.Equals("Root") &&
                handMenuRadial.RootLayer.items.ElementAt(0).name.Equals("Cancel") &&
                handMenuRadial.RootLayer.items.ElementAt(0).action.ToString().Equals("Close"))
            {
                Log.Info("Test_HandMenuRadial_NullMenuLayers: Pass!");
                return true;
            } else
            {
                Log.Err("HandMenuRadial root layer was incorrectly set");
                return false;
            }
        } catch (System.NullReferenceException e)
        {
            Log.Err("HandMenuRadial threw a Null Pointer Exception");
            return false;
        }

    }

    bool Test_HandMenuRadial_EmptyLayerName()
    {
        HandMenuRadial handMenuRadial;
        try
        {

            handMenuRadial = new HandMenuRadial(
            new HandRadialLayer("Root",
                new HandMenuItem("File", null, null, ""),
                new HandMenuItem("Search", null, null, ""),
                new HandMenuItem("About", Sprite.FromFile("search.png"), () => Log.Info(SK.VersionName)),
                new HandMenuItem("Cancel", null, null)),
            new HandRadialLayer("File",
                new HandMenuItem("New", null, () => Log.Info("New")),
                new HandMenuItem("Open", null, () => Log.Info("Open")),
                new HandMenuItem("Close", null, () => Log.Info("Close")),
                new HandMenuItem("Back", null, null, HandMenuAction.Back)),
            new HandRadialLayer("Edit",
                new HandMenuItem("Copy", null, () => Log.Info("Copy")),
                new HandMenuItem("Paste", null, () => Log.Info("Paste")),
                new HandMenuItem("Back", null, null, HandMenuAction.Back)));

        }
        catch (System.NullReferenceException e)
        {
            Log.Err("HandMenuRadial threw a Null Pointer Exception");
            return false;
        }

        if (handMenuRadial.RootLayer.layerName.Equals("Root") &&
            handMenuRadial.RootLayer.items.ElementAt(0).name.Equals("File") &&
            handMenuRadial.RootLayer.items.ElementAt(0).layerName.Equals("") &&
            handMenuRadial.RootLayer.items.ElementAt(0).action.ToString().Equals("Layer"))
        {
            if (handMenuRadial.RootLayer.FindChild("File") == null)
            {
                if (handMenuRadial.RootLayer.FindChild("Edit") == null)
                {
                    Log.Info("Test_HandMenuRadial_TwoLayers_OneItemEach: Pass!");
                    return true;
                }
                else
                {
                    Log.Err("HandMenuRadial child layers was set despite empty root layerName");
                    return false;
                }
            }
            else
            {
                Log.Err("HandMenuRadial child layer was set despite empty root layerName");
                return false;
            }

        }
        else
        {
            Log.Err("HandMenuRadial root layer was incorrectly set");
            return false;
        }
    }

    bool Test_HandRadialLayer_NoBackAction()
    {
        HandRadialLayer handRadialLayer = new HandRadialLayer("Root",
            new HandMenuItem("File", null, null, "File"),
            new HandMenuItem("Search", null, null, "Edit"),
            new HandMenuItem("About", Sprite.FromFile("search.png"), () => Log.Info(SK.VersionName)),
            new HandMenuItem("Cancel", null, null));
        if (handRadialLayer.backAngle == 0)
        {
            Log.Info("Test_HandRadialLayer_NoBackAction: Pass!");
            return true;
        } else
        {
            Log.Err("Radial Layer back angle is expected to be 0 when there are no Back actions");
            return false;
        }
    }

    bool Test_HandRadialLayer_WithBackAction()
    {
        HandRadialLayer handRadialLayer = new HandRadialLayer("Root",
            new HandMenuItem("File", null, null, "File"),
            new HandMenuItem("Search", null, null, "Edit"),
            new HandMenuItem("About", Sprite.FromFile("search.png"), () => Log.Info(SK.VersionName)),
            new HandMenuItem("Cancel", null, null, HandMenuAction.Back));
        if (handRadialLayer.backAngle == 315f)
        {
            Log.Info("Test_HandRadialLayer_WithBackAction: Pass!");
            return true;
        }
        else
        {
            Log.Err(handRadialLayer.backAngle.ToString());
            Log.Err("Radial Layer back angle is expected to be 315 for 4 items with back action on 4th item");
            return false;
        }
    }

    bool Test_HandRadialLayer_EmptyItems()
    {
        HandMenuItem[] handMenuItems = new HandMenuItem[0];
        HandRadialLayer handRadialLayer;
        
        try
        {
            handRadialLayer = new HandRadialLayer("Root", handMenuItems);
            if (handMenuItems.Length == 0)
            {
                Log.Info("Test_HandRadialLayer_EmptyItems: Pass!");
                return true;
            } else
            {
                Log.Err("MenuItem List was unexpectly altered");
                return false;
            }

        } catch (NullReferenceException e)
        {
            Log.Err("HandRadialLayer threw a Null Pointer Exception");
            return false;
        }
    }



    bool Test_HandRadialLayer_NullItems()
    {
        HandMenuItem[] handMenuItems = null;
        HandRadialLayer handRadialLayer;

        try
        {
            handRadialLayer = new HandRadialLayer("Root", handMenuItems);
            if (handMenuItems.Length == 0)
            {
                Log.Info("Test_HandRadialLayer_NullItems: Pass!");
                return true;
            }
            else
            {
                Log.Err("MenuItem List was unexpectly altered");
                return false;
            }

        }
        catch (NullReferenceException e)
        {
            Log.Err("HandRadialLayer threw a Null Pointer Exception");
            return false;
        }
    }

    bool Test_HandRadialLayer_RemoveItem()
    {
        String name = "File";
        HandRadialLayer handRadialLayer = new HandRadialLayer("Root",
            new HandMenuItem(name, null, null, name),
            new HandMenuItem("Search", null, null, "Edit"),
            new HandMenuItem("About", Sprite.FromFile("search.png"), () => Log.Info(SK.VersionName)),
            new HandMenuItem("Cancel", null, null));

        if (handRadialLayer.FindItem(name) != null)
        {
            handRadialLayer.RemoveItem(handRadialLayer.FindItem(name));

            if (handRadialLayer.FindItem(name) == null)
            {
                Log.Info("Test_HandRadialLayer_RemoveItem: Pass!");
                return true;
            }
            else
            {
                Log.Err("Failed to remove item from Menu Item array");
                return false;
            }
        } else
        {
            Log.Err("Test was skipped: HandRadialLayer Constructor Broken");
            return false;
        }
    }

    bool Test_HandRadialLayer_RemoveItem_Last()
    {
        String name = "Last";
        HandRadialLayer handRadialLayer = new HandRadialLayer("Root",
            new HandMenuItem("File", null, null, "File"),
            new HandMenuItem("Search", null, null, "Edit"),
            new HandMenuItem("About", Sprite.FromFile("search.png"), () => Log.Info(SK.VersionName)),
            new HandMenuItem("Cancel", null, null),
            new HandMenuItem(name, null, null, name));

        if (handRadialLayer.FindItem(name) != null)
        {
            handRadialLayer.RemoveItem(handRadialLayer.FindItem(name));

            if (handRadialLayer.FindItem(name) == null)
            {
                Log.Info("Test_HandRadialLayer_RemoveItem_Last: Pass!");
                return true;
            }
            else
            {
                Log.Err("Failed to remove item from Menu Item array");
                return false;
            }
        }
        else
        {
            Log.Err("Test was skipped: HandRadialLayer Constructor Broken");
            return false;
        }
    }

    bool Test_HandRadialLayer_RemoveItem_NonExistent()
    {
        String name = "Nope";
        HandRadialLayer handRadialLayer = new HandRadialLayer("Root",
            new HandMenuItem("File", null, null, "File"),
            new HandMenuItem("Search", null, null, "Edit"),
            new HandMenuItem("About", Sprite.FromFile("search.png"), () => Log.Info(SK.VersionName)),
            new HandMenuItem("Cancel", null, null));

        if (handRadialLayer.FindItem(name) == null)
        {
            handRadialLayer.RemoveItem(new HandMenuItem(name, null, null, name));

            if (handRadialLayer.FindItem(name) == null)
            {
                Log.Info("Test_HandRadialLayer_RemoveItem_NonExistent: Pass!");
                return true;
            }
            else
            {
                Log.Err("Remove Item added the item to the array instead");
                return false;
            }
        }
        else
        {
            Log.Err("Test was skipped: HandRadialLayer Constructor Broken");
            return false;
        }
    }



    public void Initialize()
    {
        //Run Tests
        Tests.Test(Test_HandMenuRadial_ThreeLayers_MultipleItems);
        Tests.Test(Test_HandMenuRadial_NullLayers);
        Tests.Test(Test_HandMenuRadial_EmptyLayerName);
        Tests.Test(Test_HandRadialLayer_NoBackAction);
        Tests.Test(Test_HandRadialLayer_WithBackAction);
        Tests.Test(Test_HandRadialLayer_EmptyItems);
        Tests.Test(Test_HandRadialLayer_RemoveItem);
        Tests.Test(Test_HandRadialLayer_RemoveItem_Last);
        Tests.Test(Test_HandRadialLayer_RemoveItem_NonExistent);
    }

    public void Shutdown()
    {
    }

    public void Step()
    {
    }
}