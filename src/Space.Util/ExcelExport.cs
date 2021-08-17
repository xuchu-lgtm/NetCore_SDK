using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Space.Util
{
    /// <summary>
    /// Export Excel
    /// </summary>
    public class ExportExcel
    {
        /// <summary>
        /// Load a collection into a the worksheet starting from the top left row of the range.
        /// </summary>
        /// <typeparam name="T">The datatype in the collection</typeparam>
        /// <param name="collection">The collection to load</param>
        /// <param name="excelName">The excel name</param>
        /// <returns>The filled range return FileStreamResult</returns>
        public static async Task<FileStreamResult> ExportAsync<T>(IEnumerable<T> collection, string excelName = default) => await ExportAsync(collection, default, excelName);

        /// <summary>
        /// Load a collection into a the worksheet starting from the top left row of the range.
        /// </summary>
        /// <typeparam name="T">The datatype in the collection</typeparam>
        /// <param name="collection">The collection to load</param>
        /// <param name="titles">The title</param>
        /// <param name="excelName">The excel name</param>
        /// <returns>The filled range return FileStreamResult</returns>
        public static async Task<FileStreamResult> ExportAsync<T>(IEnumerable<T> collection, string[] titles = default, string excelName = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            await Task.Yield();

            var stream = new MemoryStream();
            using (var package = new ExcelPackage(stream))
            {
                var workSheet = package.Workbook.Worksheets.Add("Sheet1");

                workSheet.Row(1).Height = 20;
                workSheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                workSheet.Row(1).Style.Font.Bold = true;

                workSheet.Cells.LoadFromCollection(collection, titles == default);

                if (titles != default)
                {
                    if (workSheet.Dimension.Columns != titles.Length)
                    {
                        throw new InvalidDataException("列标题与实际导出列不匹配，请检查");
                    }

                    for (var i = 1; i <= workSheet.Dimension.Columns - 1; i++) workSheet.Cells[1, i].Value = titles[i];
                }

                package.Save();
            }

            stream.Position = 0;

            //File(stream, "application/octet-stream", excelName);
            //
            //File(newStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);

            return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet") { FileDownloadName = $"{excelName ?? DateTime.Now.ToString("yyyyMMddHHmmssfff")}.xlsx" };
        }
    }
}
