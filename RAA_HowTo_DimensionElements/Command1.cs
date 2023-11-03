#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;

#endregion

namespace RAA_HowTo_DimensionElements
{
    [Transaction(TransactionMode.Manual)]
    public class Command1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // this is a variable for the Revit application
            UIApplication uiapp = commandData.Application;

            // this is a variable for the current Revit model
            Document doc = uiapp.ActiveUIDocument.Document;

            // Select wall to dimension
            Reference pickRef = uiapp.ActiveUIDocument.Selection.PickObject(ObjectType.Element, "Select wall to dimension");
            Element selectedElem = doc.GetElement(pickRef);

            if(selectedElem is Wall)
            {
                Wall selectedWall = selectedElem as Wall;

                ReferenceArray referenceArray = new ReferenceArray();
                ReferenceArray referenceArray2 = new ReferenceArray();
                Reference r1 = null, r2 = null;

                Face wallFace = GetFace(selectedWall, selectedWall.Orientation);
                EdgeArrayArray edgeArrays = wallFace.EdgeLoops;
                EdgeArray edges = edgeArrays.get_Item(0);

                List<Edge> edgeList = new List<Edge>();
                foreach(Edge edge in edges)
                {
                    Line line = edge.AsCurve() as Line;

                    if(IsLineVertical(line) == true)
                    {
                        edgeList.Add(edge);
                    }
                }

                List<Edge> sortedEdges = edgeList.OrderByDescending(e => e.AsCurve().Length).ToList();
                r1 = sortedEdges[0].Reference;
                r2 = sortedEdges[1].Reference;

                referenceArray.Append(r1);

                // reference wall ends for overall dim
                referenceArray2.Append(r1);
                referenceArray2.Append(r2);

                List<BuiltInCategory> catList = new List<BuiltInCategory>() { BuiltInCategory.OST_Windows, BuiltInCategory.OST_Doors};
                ElementMulticategoryFilter wallFilter = new ElementMulticategoryFilter(catList);

                // get windows and doors from wall and create reference
                List<ElementId> wallElemsIds = selectedWall.GetDependentElements(wallFilter).ToList();

                foreach(ElementId elemId in wallElemsIds)
                {
                    FamilyInstance curFI = doc.GetElement(elemId) as FamilyInstance;
                    Reference curRef = GetSpecialFamilyReference(curFI, SpecialReferenceType.CenterLR);
                    //Reference curRef = GetSpecialFamilyReference(curFI, SpecialReferenceType.Left);
                    //Reference curRef2 = GetSpecialFamilyReference(curFI, SpecialReferenceType.Right);
                    referenceArray.Append(curRef);
                    //referenceArray.Append(curRef2);
                }

                referenceArray.Append(r2);

                // create dimension line
                LocationCurve wallLoc = selectedWall.Location as LocationCurve;
                Line wallLine = wallLoc.Curve as Line;

                XYZ offset1 = GetOffsetByWallOrientation(wallLine.GetEndPoint(0), selectedWall.Orientation, 5);
                XYZ offset2 = GetOffsetByWallOrientation(wallLine.GetEndPoint(1), selectedWall.Orientation, 5);

                XYZ offset1b = GetOffsetByWallOrientation(wallLine.GetEndPoint(0), selectedWall.Orientation, 10);
                XYZ offset2b = GetOffsetByWallOrientation(wallLine.GetEndPoint(1), selectedWall.Orientation, 10);

                Line dimLine = Line.CreateBound(offset1, offset2);
                Line dimLine2 = Line.CreateBound(offset1b, offset2b);

                using (Transaction t = new Transaction(doc))
                {
                    t.Start("Create new dimension");

                    Dimension newDim = doc.Create.NewDimension(doc.ActiveView, dimLine, referenceArray);

                    if (wallElemsIds.Count > 0)
                    {
                        Dimension newDim2 = doc.Create.NewDimension(doc.ActiveView, dimLine2, referenceArray2);
                    }
                    
                    t.Commit();
                }
            }
            else
            {
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        private XYZ GetOffsetByWallOrientation(XYZ point, XYZ orientation, int value)
        {
            XYZ newVector = orientation.Multiply(value);
            XYZ returnPoint = point.Add(newVector);

            return returnPoint;
        }

        public enum SpecialReferenceType
        {
            Left = 0,
            CenterLR = 1,
            Right = 2,
            Front = 3,
            CenterFB = 4,
            Back = 5,
            Bottom = 6,
            CenterElevation = 7,
            Top = 8
        }

        private Reference GetSpecialFamilyReference(FamilyInstance inst, SpecialReferenceType refType)
        {
            // source for this method: https://thebuildingcoder.typepad.com/blog/2016/04/stable-reference-string-magic-voodoo.html

            Reference indexRef = null;

            int idx = (int)refType;

            if (inst != null)
            {
                Document dbDoc = inst.Document;

                Options geomOptions = new Options();
                geomOptions.ComputeReferences = true;
                geomOptions.DetailLevel = ViewDetailLevel.Undefined;
                geomOptions.IncludeNonVisibleObjects = true;

                GeometryElement gElement = inst.get_Geometry(geomOptions);
                GeometryInstance gInst = gElement.First() as GeometryInstance;

                String sampleStableRef = null;

                if (gInst != null)
                {
                    GeometryElement gSymbol = gInst.GetSymbolGeometry();

                    if (gSymbol != null)
                    {
                        foreach (GeometryObject geomObj in gSymbol)
                        {
                            if (geomObj is Solid)
                            {
                                Solid solid = geomObj as Solid;

                                if (solid.Faces.Size > 0)
                                {
                                    Face face = solid.Faces.get_Item(0);
                                    sampleStableRef = face.Reference.ConvertToStableRepresentation(dbDoc);
                                    break;
                                }
                            }
                            else if (geomObj is Curve)
                            {
                                Curve curve = geomObj as Curve;
                                Reference curveRef = curve.Reference;
                                if (curveRef != null)
                                {
                                    sampleStableRef = curve.Reference.ConvertToStableRepresentation(dbDoc);
                                    break;
                                }

                            }
                            else if (geomObj is Point)
                            {
                                Point point = geomObj as Point;
                                sampleStableRef = point.Reference.ConvertToStableRepresentation(dbDoc);
                                break;
                            }
                        }
                    }

                    if (sampleStableRef != null)
                    {
                        String[] refTokens = sampleStableRef.Split(new char[] { ':' });

                        String customStableRef = refTokens[0] + ":"
                          + refTokens[1] + ":" + refTokens[2] + ":"
                          + refTokens[3] + ":" + idx.ToString();

                        indexRef = Reference.ParseFromStableRepresentation(dbDoc, customStableRef);

                        GeometryObject geoObj = inst.GetGeometryObjectFromReference(indexRef);

                        if (geoObj != null)
                        {
                            String finalToken = "";
                            if (geoObj is Edge)
                            {
                                finalToken = ":LINEAR";
                            }

                            if (geoObj is Face)
                            {
                                finalToken = ":SURFACE";
                            }

                            customStableRef += finalToken;
                            indexRef = Reference.ParseFromStableRepresentation(dbDoc, customStableRef);
                        }
                        else
                        {
                            indexRef = null;
                        }
                    }
                }
                else
                {
                    throw new Exception("No Symbol Geometry found...");
                }
            }
            return indexRef;
        }

        private bool IsLineVertical(Line line)
        {
            if (line.Direction.IsAlmostEqualTo(XYZ.BasisZ) || line.Direction.IsAlmostEqualTo(-XYZ.BasisZ))
                return true;
            else
                return false;
        }

        private Face GetFace(Element selectedElem, XYZ orientation)
        {
            PlanarFace returnFace = null;
            List<Solid> solids = GetSolids(selectedElem);

            foreach(Solid solid in solids)
            {
                foreach(Face face in solid.Faces)
                {
                    if(face is PlanarFace)
                    {
                        PlanarFace pf = face as PlanarFace;

                        if (pf.FaceNormal.IsAlmostEqualTo(orientation))
                            returnFace = pf;
                    }
                }
            }

            return returnFace; 
        }

        private List<Solid> GetSolids(Element selectedElem)
        {
            List<Solid> returnList = new List<Solid>();

            Options options = new Options();
            options.ComputeReferences = true;
            options.DetailLevel = ViewDetailLevel.Fine;

            GeometryElement geomElem = selectedElem.get_Geometry(options);

            foreach(GeometryObject geomObj in geomElem)
            {
                if(geomObj is Solid)
                {
                    Solid solid = (Solid)geomObj;
                    if(solid.Faces.Size > 0 && solid.Volume > 0.0)
                    {
                        returnList.Add(solid);
                    }
                }
            }

            return returnList;
        }

        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnCommand1";
            string buttonTitle = "Button 1";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This is a tooltip for Button 1");

            return myButtonData1.Data;
        }
    }
}
