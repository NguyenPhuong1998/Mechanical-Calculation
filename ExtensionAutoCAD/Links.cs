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

//string subportforder = Assembly.GetExecutingAssembly().Location; 
//partDwg = System.IO.Path.Combine(subportforder, @"Drawings\Moment.dwg");

namespace MechanicalCalculation.ExtensionAutoCAD
{
    class Links
    {
        // Lệnh thêm một liên kết
        protected void AddLink(string nameOfTheLink, string nameOfBlock)
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Thêm vật 1
            if (!SeclectTwoEntityAndOnePoint(nameOfTheLink, out PromptEntityResult obj1, out PromptEntityResult obj2, out Point3d point))
            {
                ed.WriteMessage("\nDa huy lenh them " + nameOfTheLink + ".");
                return;
            }

            //Thêm vật 2
            if (nameOfBlock != "Hinged" && nameOfBlock != "Culit" && nameOfBlock != "Slip" && nameOfBlock != "Fixed")
            {
                ed.WriteMessage("\nDa huy lenh them " + nameOfTheLink + ".");
                return;
            }

            //Thêm góc nghiêng
            double angle = 0;
            if (nameOfBlock != "Hinged")
            {
                PromptAngleOptions pdo = new PromptAngleOptions("\nNhap goc nghieng cua lien ket:");
                pdo.AllowArbitraryInput = true;
                pdo.AllowNone = true;
                pdo.AllowZero = true;
                pdo.DefaultValue = 0;
                pdo.UseDefaultValue = true;
                PromptDoubleResult result = ed.GetAngle(pdo);
                if (result.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nDa huy lenh them " + nameOfTheLink + ".");
                    return;
                }
                angle = result.Value;
            }

            // Thêm vật vào bản vẽ
            string partDwg = @"Drawings\" + nameOfBlock + ".dwg";
            if (Common.InsertDrawing(partDwg, point, angle, out ObjectId IdEntity) == false)
            {
                ed.WriteMessage("\nDa huy lenh them " + nameOfTheLink + ".");
                return;
            }

            SetXData(IdEntity, obj1.ObjectId, obj2.ObjectId);
        }

        // Lấy về 2 vật thể và 1 điểm
        private bool SeclectTwoEntityAndOnePoint(string nameOfObject, out PromptEntityResult obj1, out PromptEntityResult obj2, out Point3d point)
        {
            // Get the current database and start a transaction
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            obj2 = null;
            point = default;

            obj1 = ed.GetEntity("\nChon vat 1 cua lien ket voi " + nameOfObject);
            if (obj1.Status != PromptStatus.OK)
            {
                return false;
            }

            obj2 = ed.GetEntity("\nChon vat 2 cua lien ket voi " + nameOfObject);
            if (obj2.Status != PromptStatus.OK)
            {
                return false;
            }

            var _point = ed.GetPoint(new PromptPointOptions("\nChon diem dat cua " + nameOfObject));
            if (_point.Status != PromptStatus.OK)
            {
                return false;
            }
            point = _point.Value;

            return true;
        }

        // Thêm XData
        private void SetXData(ObjectId IdEntity, ObjectId obj1, ObjectId obj2)
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity objects = new Entity();
                Common.AddRegAppTableRecord("Data_MechanicalCalculation");
                DBObject obj = tr.GetObject(IdEntity, OpenMode.ForWrite);
                TypedValue typedValue = new TypedValue(1000, Common.ObjectIdToString(obj1) + "," + Common.ObjectIdToString(obj2));
                ResultBuffer rb = new ResultBuffer(new TypedValue(1001, "Data_MechanicalCalculation"), typedValue);
                obj.XData = rb;
                rb.Dispose();
                tr.Commit();
            }
        }

        // Xử lý các dữ liệu từ XData
        public bool GetXData(BlockReference link, out DBObject object1, out DBObject object2)
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            bool nameOK = false, dataOK = false;
            object1 = null;
            object2 = null;

            ResultBuffer rb = link.XData;
            if (rb == null)
            {
                ed.WriteMessage("\nLien ket khong co du lieu, hay xoa lien ket là them lai (khong co XData).");
                return false;
            }

            foreach (TypedValue tv in rb)   // Duyệt từng dữ liệu trong XData
            {
                // Đây là tên của XData
                if (tv.TypeCode == 1001)
                {
                    if (tv.Value.ToString() == "Data_MechanicalCalculation")
                    {
                        nameOK = true;
                        if (dataOK == true) break;
                    }
                    else
                    {
                        ed.WriteMessage("\nDu lieu lien ket sai, hay xoa lien ket là them lai (XData khong dung).");
                        return false;
                    }
                }
                // Đây là dữ liệu string của XData
                if (tv.TypeCode == 1000)
                {
                    string[] arrStringData = tv.Value.ToString().Split(',');
                    if (arrStringData.Length != 2)
                    {
                        ed.WriteMessage("\nDu lieu lien ket sai, hay xoa lien ket là them lai (XData khong dung).");
                        return false;
                    }
                    if (arrStringData[0] == arrStringData[1])
                    {
                        ed.WriteMessage("\nMot lien ket khong the lien ket cung mot vat.");
                        return false;
                    }

                    using (Transaction trans = db.TransactionManager.StartTransaction())
                    {
                        ObjectId objectId;
                        if (Common.StringToObjectID(arrStringData[0], out objectId))
                        {
                            object1 = trans.GetObject(objectId, OpenMode.ForRead);
                        }
                        else
                        {
                            ed.WriteMessage("Du lieu XData bi sai, khong xac nhan duoc vat the lin ket");
                            return false;
                        }

                        if (Common.StringToObjectID(arrStringData[1], out objectId))
                        {
                            object2 = trans.GetObject(objectId, OpenMode.ForRead);
                        }
                        else
                        {
                            ed.WriteMessage("Du lieu XData bi sai, khong xac nhan duoc vat the lin ket");
                            return false;
                        }
                        trans.Commit();
                    }
                    dataOK = true;
                    if (nameOK == true) break;
                }
            }
            rb.Dispose();

            if (!(nameOK && dataOK))
            {
                ed.WriteMessage("\nDu lieu lien ket sai, hay xoa lien ket là them lai (XData khong dung).");
                return false;
            }

            return true;
        }

        public bool GetXData(BlockReference link, List<DBObject> list_object, List<BlockReference> list_ground, out int numberOfObject1, out int numberOfObject2)
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            numberOfObject1 = 0;
            numberOfObject2 = 0;

            bool nameOK = false, dataOK = false;

            ResultBuffer rb = link.XData;
            if (rb == null)
            {
                ed.WriteMessage("\nLien ket khong co du lieu, hay xoa lien ket là them lai (khong co XData).");
                return false;
            }

            foreach (TypedValue tv in rb)   // Duyệt từng dữ liệu trong XData
            {
                // Nếu là tên của XData
                if (tv.TypeCode == 1001)
                {
                    if (tv.Value.ToString() == "Data_MechanicalCalculation")
                    {
                        nameOK = true;
                        if (dataOK == true) break;
                    }
                    else
                    {
                        ed.WriteMessage("\nDu lieu lien ket sai, hay xoa lien ket là them lai (XData khong dung).");
                        return false;
                    }
                }
                // Nếu là dữ liệu string của XData
                if (tv.TypeCode == 1000)
                {
                    string[] arrStringData = tv.Value.ToString().Split(',');
                    if (arrStringData.Length != 2)
                    {
                        ed.WriteMessage("\nDu lieu lien ket sai, hay xoa lien ket là them lai (XData khong dung).");
                        return false;
                    }
                    if (arrStringData[0] == arrStringData[1])
                    {
                        ed.WriteMessage("\nMot lien ket khong the lien ket cung mot vat.");
                        return false;
                    }

                    int count = 0;
                    for (int i = 1; i < list_object.Count; i++)
                    {
                        DBObject temp = list_object[i];
                        if (temp.ObjectId.ToString() == arrStringData[0])
                        {
                            numberOfObject1 = i;
                            count++;
                        }
                        if (temp.ObjectId.ToString() == arrStringData[1])
                        {
                            numberOfObject2 = i;
                            count++;
                        }
                        if (count == 2) break;
                    }
                    foreach (var temp in list_ground)
                    {
                        if (temp.ObjectId.ToString() == arrStringData[0])
                        {
                            numberOfObject1 = 0;
                            count++;
                        }
                        if (temp.ObjectId.ToString() == arrStringData[1])
                        {
                            numberOfObject2 = 0;
                            count++;
                        }
                        if (count == 2) break;
                    }

                    if (count == 2)
                    {
                        dataOK = true;
                        if (nameOK == true) break;
                    }
                    else
                    {
                        ed.WriteMessage("\nLien ket voi mot vat khong xac dinh, hay kiem tra lai cac vat da lien ket.");
                        return false;
                    }
                }
            }
            rb.Dispose();

            if (!(nameOK && dataOK))
            {
                ed.WriteMessage("\nDu lieu lien ket sai, hay xoa lien ket là them lai (XData khong dung).");
                return false;
            }

            return true;
        }
    }

    class Hinged : Links
    {
        public void Add()
        {
            AddLink("khop quay", "Hinged");
        }
    }

    class Culit : Links
    {
        public void Add()
        {
            AddLink("khop culit", "Culit");
        }
    }

    class Slip : Links
    {
        public void Add()
        {
            AddLink("khop truot", "Slip");
        }
    }

    class Fixed : Links
    {
        public void Add()
        {
            AddLink("khop ngam", "Fixed");
        }
    }
}
