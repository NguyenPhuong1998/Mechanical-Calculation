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
    class Entity
    {
        // Lệnh thêm một bản vẽ
        public virtual void Add()
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            ed.WriteMessage("\nNhap file");
            if (!Common.GetDwgFile(out string partDwg)) return;

            string blockname = Path.GetFileNameWithoutExtension(partDwg);
            if (!GetPointAngle(blockname, out Point3d point, out double angle)) return;

            if (!Common.InsertDrawing(partDwg, new Scale3d(1, 1, 1), point, angle, out _)) return;
        }

        // Chọn 1 điểm và góc nghiêng và trả về là có thành công hay không
        protected bool GetPointAngle(string nameOfObject, out Point3d point, out double angle)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            point = default;
            angle = 0;

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

            point = _point.Value;
            angle = _angle.Value;

            return true;
        }
    }

    class Ground : Entity
    {
        public override void Add()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            if (!GetPointAngle("dat", out Point3d point, out double angle)) return;

            string partDwg = @"Drawings\Ground.dwg";
            if (Common.InsertDrawing(partDwg, point, angle, out _) == false)
            {
                ed.WriteMessage("\nDa huy lenh them dat.");
                return;
            }
        }
    }
}
