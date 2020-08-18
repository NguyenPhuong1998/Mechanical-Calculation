using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace MechanicalCalculation.ExtensionAutoCAD
{
    class Calculate
    {
        private SelectionSet selection;
        private List<DBObject> list_object = new List<DBObject>();
        private List<BlockReference> list_link = new List<BlockReference>();
        private List<BlockReference> list_external_forces = new List<BlockReference>();
        private List<BlockReference> list_ground = new List<BlockReference>();

        public void CalculateStatics()
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            SeclectObjects();
            ClassifyEntity();
            CreateDeclare(out string code);

            ed.WriteMessage(code);
        }

        // Hàm chọn các đối tượng tính toán
        private bool SeclectObjects()
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            selection = null;

            PromptSelectionOptions prSelOpts = new PromptSelectionOptions();
            prSelOpts.AllowDuplicates = false;
            prSelOpts.MessageForAdding = "\nChon doi tuong de them vao co he:";
            prSelOpts.MessageForRemoval = "\nChon doi tuong de loai khoi co he:";
            prSelOpts.RejectObjectsFromNonCurrentSpace = false;
            prSelOpts.RejectObjectsOnLockedLayers = true;
            prSelOpts.RejectPaperspaceViewport = true;

            PromptSelectionResult prSelResult = ed.GetSelection(prSelOpts);
            if (prSelResult.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nDa huy lenh tinh vi lua chon khong thanh cong");
                return false;
            }
            selection = prSelResult.Value;
            return true;
        }

        // Phân loại các đối tượng đã nhập
        private bool ClassifyEntity()
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            list_object.Clear();
            list_ground.Clear();
            list_link.Clear();
            list_external_forces.Clear();

            // Thêm 1 line số 0, coi nó là 1 vật số 0 tức là đất
            list_object.Add(new Line(new Point3d(1, 0, 0), new Point3d(-1, 0, 0)));

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                DBObject temp;
                foreach (SelectedObject objSelection in selection)
                {
                    temp = trans.GetObject(objSelection.ObjectId, OpenMode.ForRead);

                    if (temp is Line)
                    {
                        Line line = temp as Line;
                        list_object.Add(line);
                    }
                    else if (temp is BlockReference)
                    {
                        BlockReference block = temp as BlockReference;
                        switch (block.Name)
                        {
                            case "Hinged":
                            case "Culit":
                            case "Slip":
                            case "Fixed":
                                list_link.Add(block);
                                break;
                            case "Force":
                            case "MomentForward":
                            case "MomentInverse":
                                list_external_forces.Add(block);
                                break;
                            case "Ground":
                                list_ground.Add(block);
                                break;
                            default:
                                string s = block.Name.Trim().ToLower();
                                if (s.StartsWith("ground"))
                                {
                                    list_ground.Add(block);
                                    break;
                                }
                                else if (s.StartsWith("DistributedLoad"))
                                {
                                    list_external_forces.Add(block);
                                    break;
                                }
                                else
                                {
                                    list_object.Add(block);
                                    break;
                                }
                        }
                    }
                    else
                    {
                        ed.WriteMessage("\nCo lua chon khong hop le (co lua chon ngoai kieu Line va BlockReference).");
                        return false;
                    }
                }
                ed.WriteMessage("\nCo: " + list_object.Count + " vat, " + list_ground.Count + " dat, " + list_link.Count + " lien ket (" + (list_object.Count + list_ground.Count + list_link.Count) + " doi tuong)");
            }
            return true;
        }

        // Khởi tạo các khai báo
        private bool CreateDeclare(out string code)
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            code = "NumberOfObjects(" + list_object.Count + ");\n";
            string temp;

            if (!DeclareLinks(out temp)) return false;
            code += temp;
            if (!DeclareExternalForces(out temp)) return false;
            code += temp;

            return true;
        }

        // Khởi tạo các khai báo liên kết
        private bool DeclareLinks(out string code)
        {
            code = "";

            // Lấy kính vị trí, góc nghiêng của các đối tượng lưu vào 1 chuỗi
            Point3d[] arrPointOfObjects = new Point3d[list_object.Count];
            double[] arrAngleOfObjects = new double[list_object.Count];
            for (int i = 0; i < list_object.Count; i++)
            {
                if (!GetValuePointAndAngle(list_object[i], out arrPointOfObjects[i], out arrAngleOfObjects[i])) return false;
            }

            foreach (var link in list_link)
            {
                // Lấy dữ liệu từ XData
                Links temp = new Links();
                temp.GetXData(link, list_object, list_ground, out int numberOfObject1, out int numberOfObject2);

                // Liên kết
                GetValuePointAndAngle(link, out Point3d point0, out double angle0);

                // Vật 1
                Point3d point1 = arrPointOfObjects[numberOfObject1];
                double angle1 = arrAngleOfObjects[numberOfObject1];
                Point3d point01 = new Point3d(
                    Math.Round(Math.Cos(angle1) * point0.X + Math.Sin(angle1) * point0.Y - Math.Sin(angle1) * point1.Y - point1.X * Math.Cos(angle1), Common.numberOfMathRound),
                    Math.Round(-Math.Sin(angle1) * point0.X + Math.Cos(angle1) * point0.Y - Math.Cos(angle1) * point1.Y + point1.X * Math.Sin(angle1), Common.numberOfMathRound),
                    0
                    );
                double angle01 = -angle1 + angle0;

                // Vật 2
                Point3d point2 = arrPointOfObjects[numberOfObject2];
                double angle2 = arrAngleOfObjects[numberOfObject2];
                Point3d point02 = new Point3d(
                   Math.Round(Math.Cos(angle2) * point0.X + Math.Sin(angle2) * point0.Y - Math.Sin(angle2) * point2.Y - point2.X * Math.Cos(angle2), Common.numberOfMathRound),
                   Math.Round(-Math.Sin(angle2) * point0.X + Math.Cos(angle2) * point0.Y - Math.Cos(angle2) * point2.Y + point2.X * Math.Sin(angle2), Common.numberOfMathRound),
                   0
                   );
                double angle02 = -angle2 + angle0;

                switch (link.Name)
                {
                    case "Hinged":
                        code += "AddHingedJoint(" + numberOfObject1 + ", 0, " + point01.X + ", " + point01.Y + ", " + numberOfObject2 + ", 0, " + point02.X + ", " + point02.Y + ");\n";
                        break;
                    case "Culit":
                        code += "AddCulit(" + numberOfObject1 + ", 0, " + point01.X + ", " + point01.Y + ", " + angle01 + ", " + numberOfObject2 + ", 0, " + point02.X + ", " + point02.Y + ");\n";
                        break;
                    case "Slip":
                        code += "AddSlipJoint(" + numberOfObject1 + ", 0, " + point01.X + ", " + point01.Y + ", " + angle01 + ", " + numberOfObject2 + ", 0, " + point02.X + ", " + point02.Y + ", " + angle02 + ");\n";
                        break;
                    case "Fixed":
                        code += "AddFixedJoint(" + numberOfObject1 + ", 0, " + point01.X + ", " + point01.Y + ", " + numberOfObject2 + ", 0, " + point02.X + ", " + point02.Y + ", " + (angle2 - angle1) + ");\n";
                        break;
                    default:
                        break;
                }
            }

            return true;
        }

        // Khởi tạo các khai báo về lực
        private bool DeclareExternalForces(out string code)
        {
            code = "";

            return true;
        }

        // DBObject là Line hoặc Block, không lấy từ các đối tượng khác
        private bool GetValuePointAndAngle(DBObject dBObject, out Point3d point, out double angle)
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            point = default;
            angle = 0;

            if (dBObject is Line)
            {
                Line line = dBObject as Line;
                point = new Point3d((line.StartPoint.X + line.EndPoint.X) / 2, (line.StartPoint.Y + line.EndPoint.Y) / 2, 0);
                angle = line.Angle;
            }
            else if (dBObject is BlockReference)
            {
                BlockReference block = dBObject as BlockReference;
                point = block.Position;
                angle = block.Rotation;
            }
            else
            {
                ed.WriteMessage("\nCo lua chon khong hop le (co lua chon ngoai kieu Line va BlockReference).");
                return false;
            }

            return true;
        }
    }
}
