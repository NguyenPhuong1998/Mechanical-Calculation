using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Win32;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace MechanicalCalculation.ExtensionAutoCAD
{
    static class Common
    {
        #region Các biến chung
        //Biến lưu giá trị scale
        public static double scale = 1;
        public static int lengthOfDistributedLoad = 50;
        public const int numberOfMathRound = 10;
        #endregion

        #region Các hàm chung
        // Mở một file Dwg
        public static bool GetDwgFile(out string fileName)
        {
            fileName = null;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Drawing (*.dwg)|*.dwg";
            if (openFileDialog.ShowDialog() == true)
            {
                fileName = openFileDialog.FileName;
            }
            else return false;

            return true;
        }

        // Thêm 1 block chứa bản vẽ của 1 file dwg vào bản vẽ hiện tại (có scale riêng)
        public static bool InsertDrawing(string partDwg, Scale3d scale, Point3d ipt, double angle, out ObjectId objectId)
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database curdb = doc.Database;   // Biến database của bản vẽ hện tại
            Editor ed = doc.Editor;
            string blockname = Path.GetFileNameWithoutExtension(partDwg);
            bool result = false;
            objectId = ObjectId.Null;

            using (DocumentLock loc = doc.LockDocument())
            {
                ObjectId blkid = ObjectId.Null; // Biến lấy ID của file đọc vào
                try
                {
                    using (Database db = new Database(false, true))// Biến lấy database của file nạp vào
                    {
                        db.ReadDwgFile(partDwg, System.IO.FileShare.Read, true, "");   // Lấy database
                        blkid = curdb.Insert(partDwg, db, true);   // Lấy ID
                    }
                }
                catch (IOException)
                {
                    return false;
                }

                using (Transaction tr = doc.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(curdb.BlockTableId, OpenMode.ForRead);
                    if (!bt.Has(blockname))
                    {
                        bt.UpgradeOpen();
                        BlockTableRecord btrec = blkid.GetObject(OpenMode.ForRead) as BlockTableRecord;
                        btrec.UpgradeOpen();//nâng cấp để ghi
                        btrec.Name = blockname;//thêm tên
                        btrec.DowngradeOpen();//hạ cấp, không cho ghi nữa
                        bt.DowngradeOpen();
                    }
                    blkid = bt[blockname];

                    //Thêm các hình vẽ ở bản vẽ cũ vào
                    using (BlockTableRecord btr = (BlockTableRecord)curdb.CurrentSpaceId.GetObject(OpenMode.ForWrite))
                    {
                        //Insert vật thể
                        using (BlockReference bref = new BlockReference(ipt, blkid))
                        {
                            Matrix3d mat = Matrix3d.Identity;
                            bref.TransformBy(mat);
                            bref.ScaleFactors = scale;
                            bref.Rotation = angle;
                            btr.AppendEntity(bref);
                            tr.AddNewlyCreatedDBObject(bref, true);
                            bref.DowngradeOpen();
                            objectId = bref.ObjectId;
                        }
                    }

                    //Tạo lại bản vẽ???
                    ed.Regen();
                    tr.Commit();
                    result = true;
                }
            }

            return result;
        }

        // Thêm 1 block chứa bản vẽ của 1 file dwg vào bản vẽ hiện tại (dùng mặc định là scale của biến đã lưu)
        public static bool InsertDrawing(string partDwg, Point3d ipt, double angle, out ObjectId objectId)
        {
            Scale3d scale = new Scale3d(Common.scale, Common.scale, 1);
            return InsertDrawing(partDwg, scale, ipt, angle, out objectId);
        }

        // Thêm một Application Name của XData
        public static void AddRegAppTableRecord(string regAppName)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            Transaction tr = doc.TransactionManager.StartTransaction();

            using (tr)
            {
                RegAppTable rat = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead, false);

                if (!rat.Has(regAppName))
                {
                    rat.UpgradeOpen();
                    RegAppTableRecord ratr = new RegAppTableRecord();
                    ratr.Name = regAppName;
                    rat.Add(ratr);
                    tr.AddNewlyCreatedDBObject(ratr, true);
                }
                tr.Commit();
            }
        }

        // Chuyển kiểu ObjectID sang kiểu string
        public static string ObjectIdToString(ObjectId id)
        {
            return id.Handle.ToString();
        }

        // Chuyển kiểu string sang ObjectID
        public static bool StringToObjectID(string str, out ObjectId id)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            id = ObjectId.Null;
            try
            {
                long nHandle = Convert.ToInt64(str, 16);
                Handle handle = new Handle(nHandle);
                id = db.GetObjectId(false, handle, 0);
            }
            catch (FormatException)
            {
                return false;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception)
            {
                return false;
            }

            return true;
        }
        #endregion
    }
}
