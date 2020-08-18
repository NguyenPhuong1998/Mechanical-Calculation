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
    public class Commands
    {
        #region Các lệnh mô phỏng cơ hệ
        // Lệnh set scale
        [CommandMethod("SetScale")]
        public static void SetScale()
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptDoubleOptions pdo = new PromptDoubleOptions("\nNhap ty le scale:");
            pdo.AllowArbitraryInput = true;
            pdo.AllowNone = true;
            pdo.AllowZero = false;
            pdo.AllowNegative = false;
            pdo.DefaultValue = 1;
            pdo.UseDefaultValue = true;
            PromptDoubleResult _scale = ed.GetDouble(pdo);
            if (_scale.Status == PromptStatus.OK)
            {
                Common.scale = _scale.Value;
            }
        }

        // Lệnh thêm một mặt đất
        [CommandMethod("AddGround")]
        public static void AddGround()
        {
            Ground ground = new Ground();
            ground.Add();
        }

        // Lệnh thêm một đối tượng như một block
        [CommandMethod("AddObject")]
        public static void AddObject()
        {
            Entity objects = new Entity();
            objects.Add();
        }

        // Lệnh thêm một khớp quay
        [CommandMethod("AddHinged")]
        public static void AddHinged()
        {
            Hinged temp = new Hinged();
            temp.Add();
        }

        // Lệnh thêm một khớp culit
        [CommandMethod("AddCulit")]
        public static void AddCulit()
        {
            Culit temp = new Culit();
            temp.Add();
        }

        // Lệnh thêm một khớp trượt
        [CommandMethod("AddSlip")]
        public static void AddSlip()
        {
            Slip temp = new Slip();
            temp.Add();
        }

        // Lệnh thêm một khớp ngàm
        [CommandMethod("AddFixed")]
        public static void AddFixed()
        {
            Fixed temp = new Fixed();
            temp.Add();
        }

        // Lệnh thêm lực tập trung
        [CommandMethod("AddForce")]
        public static void AddForce()
        {
            Force force = new Force();
            force.Add();
        }

        // Lệnh thêm ngẫu lực
        [CommandMethod("AddMoment")]
        public static void AddMoment()
        {
            Moment moment = new Moment();
            moment.Add();
        }

        // Lệnh thêm lực phân bố
        [CommandMethod("AddDistributedLoad")]
        public static void AddDistributedLoad()
        {
            DistributedLoad distributedLoad = new DistributedLoad();
            distributedLoad.Add();
        }
        #endregion

        #region Các lệnh tính toán và mô phỏng
        [CommandMethod("CalculateStatics")]
        public static void CalculateStatics()
        {
            Calculate calculate = new Calculate();
            calculate.CalculateStatics();
        }
        #endregion

        [CommandMethod("Test")]
        public static void Test()
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            if (!Common.GetDwgFile(out string fileName)) return;
            ed.WriteMessage("\n" + fileName);
        }
    }
}
