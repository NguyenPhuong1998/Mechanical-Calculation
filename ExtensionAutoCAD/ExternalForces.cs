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

///////////string subportforder = Assembly.GetExecutingAssembly().Location; partDwg = System.IO.Path.Combine(subportforder, @"Drawings\Moment.dwg");

namespace MechanicalCalculation.ExtensionAutoCAD
{
    abstract class ExternalForces
    {
        abstract public void Add();

        protected bool GetEntityPointAngle(string nameOfObject, out ObjectId objId, out Point3d point, out double angle)
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            objId = ObjectId.Null;
            point = default;
            angle = 0;

            PromptEntityResult obj = ed.GetEntity("\nChon vat ma " + nameOfObject + " tac dong vao");
            if (obj.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nDa huy lenh them " + nameOfObject + ".");
                return false;
            }

            PromptPointResult _point = ed.GetPoint(new PromptPointOptions("\nChon diem dat cua " + nameOfObject));
            if (_point.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nDa huy lenh them " + nameOfObject + ".");
                return false;
            }

            PromptAngleOptions pdo = new PromptAngleOptions("\nNhap goc nghieng:");
            pdo.AllowArbitraryInput = true;
            pdo.AllowNone = true;
            pdo.AllowZero = true;
            pdo.DefaultValue = 0;
            pdo.UseDefaultValue = true;
            PromptDoubleResult _angle = ed.GetAngle(pdo);
            if (_angle.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nDa huy lenh them " + nameOfObject + ".");
                return false;
            }

            objId = obj.ObjectId;
            point = _point.Value;
            angle = _angle.Value;

            return true;
        }

        protected void SetXDataForForcesAndMoment(ObjectId idExternalForces, string intensity, ObjectId objectId)
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Common.AddRegAppTableRecord("Data_MechanicalCalculation");
                TypedValue typedValue = new TypedValue(1000, intensity + "," + objectId.ToString());
                ResultBuffer rb = new ResultBuffer(new TypedValue(1001, "Data_MechanicalCalculation"), typedValue);
                tr.GetObject(idExternalForces, OpenMode.ForWrite).XData = rb;
                rb.Dispose();
                tr.Commit();
            }
        }
    }

    class Force : ExternalForces
    {
        public override void Add()
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var intensity = ed.GetString(new PromptStringOptions("Nhap gia tri luc"));
            if (intensity.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nDa huy lenh them luc.");
                return;
            }

            if (!GetEntityPointAngle("luc", out ObjectId objectId, out Point3d point, out double angle)) return;

            string partDwg = @"Drawings\Force.dwg";
            if (Common.InsertDrawing(partDwg, point, angle, out ObjectId idForce) == false)
            {
                ed.WriteMessage("\nDa huy lenh them luc.");
                return;
            }

            SetXDataForForcesAndMoment(idForce, intensity.StringResult, objectId);
        }
    }

    class Moment : ExternalForces
    {
        public override void Add()
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            PromptKeywordOptions prDisOption = new PromptKeywordOptions("Moment thuan hay nghich?");
            prDisOption.Keywords.Add("Thuan");
            prDisOption.Keywords.Add("Nghich");
            var option = ed.GetKeywords(prDisOption);
            string partDwg;
            if (option.Status == PromptStatus.OK)
            {
                if (option.StringResult == "Thuan")
                    partDwg = @"Drawings\MomentForward.dwg";
                else
                    partDwg = @"Drawings\MomentInverse.dwg";
            }
            else
            {
                ed.WriteMessage("\nDa huy lenh them moment.");
                return;
            }

            var intensity = ed.GetString(new PromptStringOptions("Nhap gia tri moment"));
            if (intensity.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nDa huy lenh them moment.");
                return;
            }

            if (!GetEntityPointAngle("moment", out ObjectId objectId, out Point3d point, out double angle)) return;

            if (Common.InsertDrawing(partDwg, point, angle, out ObjectId idMoment) == false)
            {
                ed.WriteMessage("\nDa huy lenh them moment.");
                return;
            }

            SetXDataForForcesAndMoment(idMoment, intensity.StringResult, objectId);
        }
    }

    class DistributedLoad : ExternalForces
    {
        public override void Add()
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var intensity1 = ed.GetString(new PromptStringOptions("Nhap gia tri luc cua diem dau tien"));
            if (intensity1.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nDa huy lenh them luc.");
                return;
            }

            var intensity2 = ed.GetString(new PromptStringOptions("Nhap gia tri luc cua diem sau"));
            if (intensity2.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nDa huy lenh them luc.");
                return;
            }

            PromptEntityResult objectId = ed.GetEntity("\nChon vat ma luc phan bo tac dong vao");
            if (objectId.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nDa huy lenh them luc phan bo.");
                return;
            }

            PromptPointResult point1 = ed.GetPoint(new PromptPointOptions("\nChon cac diem dat luc dau tien"));
            if (point1.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nDa huy lenh them luc phan bo.");
                return;
            }

            PromptPointResult point2 = ed.GetPoint(new PromptPointOptions("\nChon cac diem dat luc thu hai"));
            if (point2.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nDa huy lenh them luc phan bo.");
                return;
            }

            PromptAngleOptions pdo = new PromptAngleOptions("\nNhap goc nghieng:");
            pdo.AllowArbitraryInput = true;
            pdo.AllowNone = true;
            pdo.AllowZero = true;
            pdo.DefaultValue = 0;
            pdo.UseDefaultValue = true;
            PromptDoubleResult angle = ed.GetAngle(pdo);
            if (angle.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nDa huy lenh them luc phan bo. Vi khong lay duoc goc.");
                return;
            }

            string partDwg = @"Drawings\DistributedLoad.dwg";

            ////////////////////////
            Point3d point = new Point3d((point1.Value.X + point2.Value.X) / 2, (point1.Value.Y + point2.Value.Y) / 2, 0);
            double length = Math.Sqrt(Math.Pow(point1.Value.X - point2.Value.X, 2) + Math.Pow(point1.Value.Y - point2.Value.Y, 1));
            if (Math.Round(length, Common.numberOfMathRound) == 0)
            {
                ed.WriteMessage("\nDa huy lenh them luc phân bo. Vì kich thuoc khong the trung nhau.");
                return;
            }
            Scale3d scale = new Scale3d(Common.scale, length / Common.lengthOfDistributedLoad, 1);

            if (Common.InsertDrawing(partDwg, scale, point, angle.Value, out ObjectId idDistributedLoad) == false)
            {
                ed.WriteMessage("\nDa huy lenh them luc phân bo. Vi khong the ve duoc ban ve.");
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Common.AddRegAppTableRecord("Data_MechanicalCalculation");
                TypedValue typedValue = new TypedValue(1000, intensity1 + "," + intensity2 + "," + objectId.ToString() + "," + point1.Value.X + "," + point1.Value.Y + "," + point2.Value.X + "," + point2.Value.Y);
                ResultBuffer rb = new ResultBuffer(new TypedValue(1001, "Data_MechanicalCalculation"), typedValue);
                tr.GetObject(idDistributedLoad, OpenMode.ForWrite).XData = rb;
                rb.Dispose();
                tr.Commit();
            }
        }
    }
}
