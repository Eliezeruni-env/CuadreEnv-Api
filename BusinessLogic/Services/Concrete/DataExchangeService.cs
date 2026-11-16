using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Exceptions;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain.DataExchange;
using Onion.Domain.Products;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class DataExchangeService : IDataExchangeService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<DataExchangeService> _logger;

        public DataExchangeService(IUnitOfWork uow, ILogger<DataExchangeService> logger)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(byte[] Content, string ContentType, string FileName)> GenerateTemplateAsync(int companyId, string entityType, string format = "xlsx")
        {
            var entity = (entityType ?? "products").Trim().ToLowerInvariant();
            var isCsv = string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase);

            if (entity == "products" || entity == "productos")
            {
                if (isCsv)
                {
                    var csvBuilder = new StringBuilder();
                    csvBuilder.AppendLine("CodigoBarras,Referencia,Descripcion,Precio,Costo,Categoria,TipoProducto,CantidadMinima");
                    csvBuilder.AppendLine("7461234567890,PROD-001,Cuaderno Universitario 200 Hojas,150.00,95.00,Papeleria,Articulo,5");
                    csvBuilder.AppendLine("7469876543210,PROD-002,Lapicero Azul Tinta Seca,25.00,12.50,Oficina,Articulo,20");
                    var bytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
                    return (bytes, "text/csv", "plantilla_productos.csv");
                }
                else
                {
                    using var wb = new XLWorkbook();
                    var ws = wb.Worksheets.Add("Productos");

                    // Headers
                    var headers = new[]
                    {
                        "CodigoBarras", "Referencia", "Descripcion", "Precio", "Costo", "Categoria", "TipoProducto", "CantidadMinima"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = ws.Cell(1, i + 1);
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.FromArgb(13, 110, 253);
                        cell.Style.Font.FontColor = XLColor.White;
                    }

                    // Sample Row 1
                    ws.Cell(2, 1).SetValue("7461234567890");
                    ws.Cell(2, 2).SetValue("PROD-001");
                    ws.Cell(2, 3).SetValue("Cuaderno Universitario 200 Hojas");
                    ws.Cell(2, 4).SetValue(150.00);
                    ws.Cell(2, 5).SetValue(95.00);
                    ws.Cell(2, 6).SetValue("Papeleria");
                    ws.Cell(2, 7).SetValue("Articulo");
                    ws.Cell(2, 8).SetValue(5);

                    // Sample Row 2
                    ws.Cell(3, 1).SetValue("7469876543210");
                    ws.Cell(3, 2).SetValue("PROD-002");
                    ws.Cell(3, 3).SetValue("Lapicero Azul Tinta Seca");
                    ws.Cell(3, 4).SetValue(25.00);
                    ws.Cell(3, 5).SetValue(12.50);
                    ws.Cell(3, 6).SetValue("Oficina");
                    ws.Cell(3, 7).SetValue("Articulo");
                    ws.Cell(3, 8).SetValue(20);

                    ws.Columns().AdjustToContents();

                    using var ms = new MemoryStream();
                    wb.SaveAs(ms);
                    return (ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla_productos.xlsx");
                }
            }

            throw new CustomException(new Onion.Common.Models.Error { Code = "UNSUPPORTED_ENTITY", Message = $"Entidad '{entityType}' no soportada para plantillas.", Language = "ES" });
        }

        public async Task<(byte[] Content, string ContentType, string FileName)> ExportDataAsync(int companyId, string entityType, string format = "xlsx")
        {
            var entity = (entityType ?? "products").Trim().ToLowerInvariant();
            var isCsv = string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase);

            if (entity == "products" || entity == "productos")
            {
                var products = (await _uow.Products.FindAsync(p => p.CompanyId == companyId && p.Active && !p.IsDeleted))
                    .OrderBy(p => p.Description)
                    .ToList();

                var categories = (await _uow.Categories.FindAsync(c => c.CompanyId == companyId && c.Active && !c.IsDeleted))
                    .ToDictionary(c => c.Id, c => c.Description);

                var productTypes = (await _uow.ProductTypes.FindAsync(pt => pt.Active && !pt.IsDeleted))
                    .ToDictionary(pt => pt.Id, pt => pt.Description);

                if (isCsv)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("CodigoBarras,Referencia,Descripcion,Precio,Costo,Categoria,TipoProducto,CantidadMinima,StockTotal");
                    foreach (var p in products)
                    {
                        var catName = categories.GetValueOrDefault(p.CategoryId, "");
                        var ptName = productTypes.GetValueOrDefault(p.ProductTypeId, "");
                        var desc = EscapeCsv(p.Description);
                        var bar = EscapeCsv(p.Barcode);
                        var code = EscapeCsv(p.Reference);
                        sb.AppendLine($"{bar},{code},{desc},{p.Price.ToString("F2", CultureInfo.InvariantCulture)},{p.Cost.ToString("F2", CultureInfo.InvariantCulture)},{EscapeCsv(catName)},{EscapeCsv(ptName)},{p.MinimumQuantity},{p.Stock}");
                    }
                    var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                    return (bytes, "text/csv", $"catalogo_productos_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
                }
                else
                {
                    using var wb = new XLWorkbook();
                    var ws = wb.Worksheets.Add("Catalogo");

                    var headers = new[]
                    {
                        "CodigoBarras", "Referencia", "Descripcion", "Precio", "Costo", "Categoria", "TipoProducto", "CantidadMinima", "StockTotal"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = ws.Cell(1, i + 1);
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.FromArgb(13, 110, 253);
                        cell.Style.Font.FontColor = XLColor.White;
                    }

                    int row = 2;
                    foreach (var p in products)
                    {
                        ws.Cell(row, 1).SetValue(p.Barcode ?? "");
                        ws.Cell(row, 2).SetValue(p.Reference ?? "");
                        ws.Cell(row, 3).SetValue(p.Description ?? "");
                        ws.Cell(row, 4).SetValue(p.Price);
                        ws.Cell(row, 5).SetValue(p.Cost);
                        ws.Cell(row, 6).SetValue(categories.GetValueOrDefault(p.CategoryId, ""));
                        ws.Cell(row, 7).SetValue(productTypes.GetValueOrDefault(p.ProductTypeId, ""));
                        ws.Cell(row, 8).SetValue(p.MinimumQuantity);
                        ws.Cell(row, 9).SetValue(p.Stock);
                        row++;
                    }

                    ws.Columns().AdjustToContents();

                    using var ms = new MemoryStream();
                    wb.SaveAs(ms);
                    return (ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"catalogo_productos_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
                }
            }

            throw new CustomException(new Onion.Common.Models.Error { Code = "UNSUPPORTED_ENTITY", Message = $"Entidad '{entityType}' no soportada para exportación.", Language = "ES" });
        }

        public async Task<ImportPreviewResponseDto> PreviewImportAsync(int companyId, string entityType, Stream fileStream, string fileName, ImportMode mode, string userId)
        {
            var entity = (entityType ?? "products").Trim().ToLowerInvariant();
            if (entity != "products" && entity != "productos")
            {
                throw new CustomException(new Onion.Common.Models.Error { Code = "UNSUPPORTED_ENTITY", Message = $"Entidad '{entityType}' no soportada en Fase 1.", Language = "ES" });
            }

            // Read file into memory and calculate hash
            byte[] fileBytes;
            using (var memory = new MemoryStream())
            {
                await fileStream.CopyToAsync(memory);
                fileBytes = memory.ToArray();
            }

            if (fileBytes.Length == 0)
            {
                throw new CustomException(new Onion.Common.Models.Error { Code = "EMPTY_FILE", Message = "El archivo cargado está vacío.", Language = "ES" });
            }

            string fileHash;
            using (var sha = SHA256.Create())
            {
                fileHash = Convert.ToHexString(sha.ComputeHash(fileBytes));
            }

            // Parse rows into standard raw dictionaries
            var isCsv = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
            List<Dictionary<string, string>> rawRows;

            if (isCsv)
            {
                rawRows = ParseCsv(fileBytes);
            }
            else
            {
                rawRows = ParseXlsx(fileBytes);
            }

            if (rawRows.Count == 0)
            {
                throw new CustomException(new Onion.Common.Models.Error { Code = "NO_DATA_ROWS", Message = "No se encontraron filas de datos en el archivo.", Language = "ES" });
            }

            // Existing catalog data for resolution
            var existingProducts = (await _uow.Products.FindAsync(p => p.CompanyId == companyId && p.Active && !p.IsDeleted)).ToList();
            var categories = (await _uow.Categories.FindAsync(c => c.CompanyId == companyId && c.Active && !c.IsDeleted)).ToList();
            var productTypes = (await _uow.ProductTypes.FindAsync(pt => pt.Active && !pt.IsDeleted)).ToList();

            var previewRows = new List<PreviewRowItemDto>();
            var errors = new List<RowErrorDto>();
            var parsedPayloadRows = new List<ParsedProductRowPayload>();

            int rowNumber = 1; // 1-based data row counter (corresponds to row 2+ in Excel)
            int rowsToInsert = 0;
            int rowsToUpdate = 0;
            int rowsUnchanged = 0;

            foreach (var dict in rawRows)
            {
                rowNumber++;
                var rowErrors = new List<string>();

                var barcode = GetField(dict, "codigobarras", "barcode", "codigo");
                var code = GetField(dict, "referencia", "code", "ref", "referenciainterna");
                var description = GetField(dict, "descripcion", "description", "nombre", "name");
                var priceStr = GetField(dict, "precio", "price", "precioventa");
                var costStr = GetField(dict, "costo", "cost", "costounitario");
                var categoryStr = GetField(dict, "categoria", "category");
                var typeStr = GetField(dict, "tipoproducto", "producttype", "tipo");
                var minQtyStr = GetField(dict, "cantidadminima", "minimumquantity", "stockminimo");

                // Validation 1: At least one stable key is required
                if (string.IsNullOrWhiteSpace(barcode) && string.IsNullOrWhiteSpace(code))
                {
                    rowErrors.Add("Fila sin identificador: Debe especificar 'CodigoBarras' o 'Referencia'.");
                    errors.Add(new RowErrorDto { RowNumber = rowNumber, Field = "CodigoBarras / Referencia", Reason = "Debe proporcionar al menos un código o referencia." });
                }

                // Check matches in existing products by Code or Barcode
                List<Product> matches = new();
                if (!string.IsNullOrWhiteSpace(code))
                {
                    matches = existingProducts.Where(p => string.Equals(p.Reference?.Trim(), code.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                }
                if (matches.Count == 0 && !string.IsNullOrWhiteSpace(barcode))
                {
                    matches = existingProducts.Where(p => string.Equals(p.Barcode?.Trim(), barcode.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Validation 2: Check ambiguity
                if (matches.Count > 1)
                {
                    var msg = $"Referencia ambigua: Existen {matches.Count} productos con la misma referencia o código de barras.";
                    rowErrors.Add(msg);
                    errors.Add(new RowErrorDto { RowNumber = rowNumber, Field = "Identificador", Reason = msg });
                }

                var existingProduct = matches.FirstOrDefault();
                var changes = new List<RowFieldChangeDto>();
                string rowAction;

                if (existingProduct != null)
                {
                    // Existing record -> UPDATE or UNCHANGED
                    if (mode == ImportMode.OnlyCreate)
                    {
                        var msg = $"El producto '{existingProduct.Description}' ya existe y el modo seleccionado es 'Solo Crear'.";
                        rowErrors.Add(msg);
                        errors.Add(new RowErrorDto { RowNumber = rowNumber, Field = "Identificador", Reason = msg });
                        rowAction = "ERROR";
                    }
                    else
                    {
                        // Check description change
                        if (!string.IsNullOrWhiteSpace(description) && !string.Equals(existingProduct.Description?.Trim(), description.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            changes.Add(new RowFieldChangeDto { FieldName = "Descripcion", OldValue = existingProduct.Description, NewValue = description.Trim() });
                        }

                        // Check price change (Empty cell keeps current value!)
                        if (!string.IsNullOrWhiteSpace(priceStr))
                        {
                            if (decimal.TryParse(priceStr.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedPrice))
                            {
                                if (parsedPrice <= 0)
                                {
                                    var msg = "El precio debe ser mayor a 0.";
                                    rowErrors.Add(msg);
                                    errors.Add(new RowErrorDto { RowNumber = rowNumber, Field = "Precio", Reason = msg });
                                }
                                else if (parsedPrice != existingProduct.Price)
                                {
                                    changes.Add(new RowFieldChangeDto { FieldName = "Precio", OldValue = existingProduct.Price.ToString("F2"), NewValue = parsedPrice.ToString("F2") });
                                }
                            }
                            else
                            {
                                var msg = $"Valor de precio inválido: '{priceStr}'";
                                rowErrors.Add(msg);
                                errors.Add(new RowErrorDto { RowNumber = rowNumber, Field = "Precio", Reason = msg });
                            }
                        }

                        // Check cost change
                        if (!string.IsNullOrWhiteSpace(costStr))
                        {
                            if (decimal.TryParse(costStr.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedCost))
                            {
                                if (parsedCost <= 0)
                                {
                                    var msg = "El costo debe ser mayor a 0.";
                                    rowErrors.Add(msg);
                                    errors.Add(new RowErrorDto { RowNumber = rowNumber, Field = "Costo", Reason = msg });
                                }
                                else if (parsedCost != existingProduct.Cost)
                                {
                                    changes.Add(new RowFieldChangeDto { FieldName = "Costo", OldValue = existingProduct.Cost.ToString("F2"), NewValue = parsedCost.ToString("F2") });
                                }
                            }
                            else
                            {
                                var msg = $"Valor de costo inválido: '{costStr}'";
                                rowErrors.Add(msg);
                                errors.Add(new RowErrorDto { RowNumber = rowNumber, Field = "Costo", Reason = msg });
                            }
                        }

                        // Check minimum quantity change
                        if (!string.IsNullOrWhiteSpace(minQtyStr))
                        {
                            if (int.TryParse(minQtyStr, out var parsedMinQty))
                            {
                                if (parsedMinQty >= 0 && parsedMinQty != existingProduct.MinimumQuantity)
                                {
                                    changes.Add(new RowFieldChangeDto { FieldName = "CantidadMinima", OldValue = existingProduct.MinimumQuantity.ToString(), NewValue = parsedMinQty.ToString() });
                                }
                            }
                        }

                        if (rowErrors.Count > 0)
                        {
                            rowAction = "ERROR";
                        }
                        else if (changes.Count > 0)
                        {
                            rowAction = "UPDATE";
                            rowsToUpdate++;
                        }
                        else
                        {
                            rowAction = "UNCHANGED";
                            rowsUnchanged++;
                        }
                    }
                }
                else
                {
                    // New record -> INSERT
                    if (string.IsNullOrWhiteSpace(description))
                    {
                        var msg = "La descripción o nombre del producto es obligatorio para nuevos registros.";
                        rowErrors.Add(msg);
                        errors.Add(new RowErrorDto { RowNumber = rowNumber, Field = "Descripcion", Reason = msg });
                    }

                    if (!decimal.TryParse(priceStr?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedPrice) || parsedPrice <= 0)
                    {
                        var msg = "El precio es obligatorio y debe ser mayor a 0 para nuevos productos.";
                        rowErrors.Add(msg);
                        errors.Add(new RowErrorDto { RowNumber = rowNumber, Field = "Precio", Reason = msg });
                    }

                    if (!decimal.TryParse(costStr?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedCost) || parsedCost <= 0)
                    {
                        var msg = "El costo es obligatorio y debe ser mayor a 0 para nuevos productos.";
                        rowErrors.Add(msg);
                        errors.Add(new RowErrorDto { RowNumber = rowNumber, Field = "Costo", Reason = msg });
                    }

                    if (rowErrors.Count > 0)
                    {
                        rowAction = "ERROR";
                    }
                    else
                    {
                        rowAction = "INSERT";
                        rowsToInsert++;
                        changes.Add(new RowFieldChangeDto { FieldName = "Descripcion", OldValue = null, NewValue = description });
                        changes.Add(new RowFieldChangeDto { FieldName = "Precio", OldValue = null, NewValue = parsedPrice.ToString("F2") });
                        changes.Add(new RowFieldChangeDto { FieldName = "Costo", OldValue = null, NewValue = parsedCost.ToString("F2") });
                    }
                }

                var keyDisplay = !string.IsNullOrWhiteSpace(code) ? code : (!string.IsNullOrWhiteSpace(barcode) ? barcode : $"Fila {rowNumber}");
                var previewItem = new PreviewRowItemDto
                {
                    RowNumber = rowNumber,
                    Key = keyDisplay,
                    Action = rowAction,
                    Description = description ?? existingProduct?.Description ?? "Sin Descripción",
                    Changes = changes,
                    Errors = rowErrors
                };
                previewRows.Add(previewItem);

                // Prepare internal payload for execution
                parsedPayloadRows.Add(new ParsedProductRowPayload
                {
                    RowNumber = rowNumber,
                    ExistingProductId = existingProduct?.Id,
                    Barcode = barcode,
                    Code = code,
                    Description = description,
                    Price = !string.IsNullOrWhiteSpace(priceStr) && decimal.TryParse(priceStr.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var p) ? p : null,
                    Cost = !string.IsNullOrWhiteSpace(costStr) && decimal.TryParse(costStr.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var c) ? c : null,
                    CategoryName = categoryStr,
                    ProductTypeName = typeStr,
                    MinimumQuantity = !string.IsNullOrWhiteSpace(minQtyStr) && int.TryParse(minQtyStr, out var mq) ? mq : null,
                    Action = rowAction
                });
            }

            var batch = new ImportBatch
            {
                CompanyId = companyId,
                EntityType = "PRODUCTS",
                Mode = mode,
                OriginalFileName = fileName,
                FileHashSha256 = fileHash,
                Status = ImportBatchStatus.Previewed,
                TotalRows = rawRows.Count,
                RowsToInsert = rowsToInsert,
                RowsToUpdate = rowsToUpdate,
                RowsUnchanged = rowsUnchanged,
                RowsWithErrors = errors.Count,
                StoredRowsJson = JsonSerializer.Serialize(parsedPayloadRows),
                ErrorsJson = errors.Count > 0 ? JsonSerializer.Serialize(errors) : null,
                PreviewedByUserId = userId,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            await _uow.ImportBatches.AddAsync(batch);
            await _uow.DataExchangeLogs.AddAsync(new DataExchangeLog
            {
                CompanyId = companyId,
                UserId = userId,
                Action = "PREVIEW",
                EntityType = "PRODUCTS",
                Format = isCsv ? "CSV" : "XLSX",
                BatchId = batch.BatchId,
                ProcessedCount = rawRows.Count,
                SuccessCount = rowsToInsert + rowsToUpdate + rowsUnchanged,
                ErrorCount = errors.Count,
                Details = $"Previsualización: {rowsToInsert} nuevos, {rowsToUpdate} actualizaciones, {rowsUnchanged} sin cambios, {errors.Count} errores."
            });
            await _uow.SaveChangesAsync();

            return new ImportPreviewResponseDto
            {
                BatchId = batch.BatchId,
                EntityType = "PRODUCTS",
                Mode = mode.ToString(),
                FileName = fileName,
                TotalRows = rawRows.Count,
                RowsToInsert = rowsToInsert,
                RowsToUpdate = rowsToUpdate,
                RowsUnchanged = rowsUnchanged,
                RowsWithErrors = errors.Count,
                ExpiresAt = batch.ExpiresAt,
                SampleRows = previewRows.Take(100).ToList(),
                Errors = errors
            };
        }

        public async Task<ImportConfirmResponseDto> ConfirmImportAsync(int companyId, Guid batchId, string userId)
        {
            await using var tx = await _uow.BeginTransactionAsync();
            try
            {
                var batches = await _uow.ImportBatches.FindAsync(b => b.BatchId == batchId && b.CompanyId == companyId);
                var batch = batches.FirstOrDefault();
                if (batch == null)
                {
                    throw new CustomException(new Onion.Common.Models.Error { Code = "BATCH_NOT_FOUND", Message = "El lote de importación no existe o no pertenece a su empresa.", Language = "ES" });
                }

                // IDEMPOTENCY CHECK: If already applied, return cached result immediately
                if (batch.Status == ImportBatchStatus.Applied)
                {
                    if (!string.IsNullOrWhiteSpace(batch.ExecutionResultJson))
                    {
                        var cached = JsonSerializer.Deserialize<ImportConfirmResponseDto>(batch.ExecutionResultJson);
                        if (cached != null)
                        {
                            cached.AlreadyApplied = true;
                            return cached;
                        }
                    }
                    return new ImportConfirmResponseDto
                    {
                        BatchId = batch.BatchId,
                        Success = true,
                        AlreadyApplied = true,
                        Message = "Este lote ya fue confirmado y aplicado previamente.",
                        InsertedCount = batch.RowsToInsert,
                        UpdatedCount = batch.RowsToUpdate,
                        AppliedAt = batch.AppliedAt
                    };
                }

                if (batch.Status == ImportBatchStatus.Expired || batch.ExpiresAt < DateTime.UtcNow)
                {
                    throw new CustomException(new Onion.Common.Models.Error { Code = "BATCH_EXPIRED", Message = "El lote ha expirado. Por favor previsualice el archivo nuevamente.", Language = "ES" });
                }

                if (batch.RowsWithErrors > 0)
                {
                    throw new CustomException(new Onion.Common.Models.Error { Code = "BATCH_HAS_ERRORS", Message = "El lote contiene errores y no puede aplicarse. Corrija el archivo y vuelva a intentarlo.", Language = "ES" });
                }

                var payloadRows = JsonSerializer.Deserialize<List<ParsedProductRowPayload>>(batch.StoredRowsJson);
                if (payloadRows == null || payloadRows.Count == 0)
                {
                    throw new CustomException(new Onion.Common.Models.Error { Code = "CORRUPT_BATCH_PAYLOAD", Message = "No hay datos de filas válidos en el lote.", Language = "ES" });
                }

                var defaultCat = (await _uow.Categories.FindAsync(c => c.CompanyId == companyId && c.Active && !c.IsDeleted)).FirstOrDefault();
                if (defaultCat == null)
                {
                    defaultCat = new Category { Description = "General", CompanyId = companyId, Active = true, IsDeleted = false };
                    await _uow.Categories.AddAsync(defaultCat);
                    await _uow.SaveChangesAsync();
                }

                var defaultType = (await _uow.ProductTypes.FindAsync(pt => pt.Active && !pt.IsDeleted)).FirstOrDefault();

                int insertedCount = 0;
                int updatedCount = 0;

                foreach (var row in payloadRows)
                {
                    if (row.Action == "UPDATE" && row.ExistingProductId.HasValue)
                    {
                        var product = await _uow.Products.GetByIdAsync(row.ExistingProductId.Value);
                        if (product == null || product.CompanyId != companyId)
                        {
                            throw new CustomException(new Onion.Common.Models.Error { Code = "CONCURRENCY_ERROR", Message = $"El producto #{row.ExistingProductId} ya no existe. El lote debe revalidarse.", Language = "ES" });
                        }

                        // Apply non-null updates (empty cells preserve current value!)
                        if (!string.IsNullOrWhiteSpace(row.Description)) product.Description = row.Description.Trim();
                        if (row.Price.HasValue && row.Price.Value > 0) product.Price = row.Price.Value;
                        if (row.Cost.HasValue && row.Cost.Value > 0) product.Cost = row.Cost.Value;
                        if (row.MinimumQuantity.HasValue && row.MinimumQuantity.Value >= 0) product.MinimumQuantity = row.MinimumQuantity.Value;
                        if (!string.IsNullOrWhiteSpace(row.Code)) product.Reference = row.Code.Trim();
                        if (!string.IsNullOrWhiteSpace(row.Barcode)) product.Barcode = row.Barcode.Trim();

                        product.ModifiedBy = userId;
                        product.ModificationDate = DateTime.UtcNow;
                        _uow.Products.Update(product);
                        updatedCount++;
                    }
                    else if (row.Action == "INSERT")
                    {
                        var newProduct = new Product
                        {
                            CompanyId = companyId,
                            Barcode = row.Barcode?.Trim(),
                            Reference = row.Code?.Trim(),
                            Description = row.Description?.Trim() ?? "Nuevo Producto",
                            Price = row.Price ?? 0,
                            Cost = row.Cost ?? 0,
                            MinimumQuantity = row.MinimumQuantity ?? 0,
                            Stock = 0,
                            CategoryId = defaultCat.Id,
                            ProductTypeId = defaultType?.Id ?? 1,
                            Active = true,
                            IsDeleted = false,
                            CreateBy = userId,
                            CreationDate = DateTime.UtcNow
                        };
                        await _uow.Products.AddAsync(newProduct);
                        insertedCount++;
                    }
                }

                await _uow.SaveChangesAsync();

                // Mark batch as Applied atomically
                batch.Status = ImportBatchStatus.Applied;
                batch.AppliedAt = DateTime.UtcNow;
                batch.AppliedByUserId = userId;

                var result = new ImportConfirmResponseDto
                {
                    BatchId = batch.BatchId,
                    Success = true,
                    Message = $"Importación aplicada exitosamente: {updatedCount} actualizados, {insertedCount} creados.",
                    InsertedCount = insertedCount,
                    UpdatedCount = updatedCount,
                    AlreadyApplied = false,
                    AppliedAt = batch.AppliedAt
                };

                batch.ExecutionResultJson = JsonSerializer.Serialize(result);
                _uow.ImportBatches.Update(batch);

                await _uow.DataExchangeLogs.AddAsync(new DataExchangeLog
                {
                    CompanyId = companyId,
                    UserId = userId,
                    Action = "CONFIRM",
                    EntityType = "PRODUCTS",
                    Format = batch.OriginalFileName.EndsWith(".csv") ? "CSV" : "XLSX",
                    BatchId = batch.BatchId,
                    ProcessedCount = payloadRows.Count,
                    SuccessCount = insertedCount + updatedCount,
                    ErrorCount = 0,
                    Details = $"Aplicado: {insertedCount} creados, {updatedCount} actualizados."
                });

                await _uow.SaveChangesAsync();
                await tx.CommitAsync();

                return result;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<(byte[] Content, string ContentType, string FileName)> ExportErrorsAsync(int companyId, Guid batchId)
        {
            var batches = await _uow.ImportBatches.FindAsync(b => b.BatchId == batchId && b.CompanyId == companyId);
            var batch = batches.FirstOrDefault();
            if (batch == null || string.IsNullOrWhiteSpace(batch.ErrorsJson))
            {
                throw new CustomException(new Onion.Common.Models.Error { Code = "NO_ERRORS", Message = "No hay reporte de errores para este lote.", Language = "ES" });
            }

            var errors = JsonSerializer.Deserialize<List<RowErrorDto>>(batch.ErrorsJson) ?? new List<RowErrorDto>();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Errores");

            ws.Cell(1, 1).SetValue("Fila");
            ws.Cell(1, 2).SetValue("Campo / Columna");
            ws.Cell(1, 3).SetValue("Motivo de Error");

            var headerRange = ws.Range(1, 1, 1, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(220, 53, 69);
            headerRange.Style.Font.FontColor = XLColor.White;

            int r = 2;
            foreach (var err in errors)
            {
                ws.Cell(r, 1).SetValue(err.RowNumber);
                ws.Cell(r, 2).SetValue(err.Field);
                ws.Cell(r, 3).SetValue(err.Reason);
                r++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return (ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"errores_lote_{batchId}.xlsx");
        }

        public async Task<List<DataExchangeLogDto>> GetHistoryAsync(int companyId)
        {
            var logs = (await _uow.DataExchangeLogs.FindAsync(l => l.CompanyId == companyId))
                .OrderByDescending(l => l.CreationDate)
                .Take(50)
                .Select(l => new DataExchangeLogDto
                {
                    Id = l.Id,
                    Action = l.Action,
                    EntityType = l.EntityType,
                    Format = l.Format,
                    BatchId = l.BatchId,
                    ProcessedCount = l.ProcessedCount,
                    SuccessCount = l.SuccessCount,
                    ErrorCount = l.ErrorCount,
                    ExecutedBy = l.UserId,
                    Date = l.CreationDate,
                    Details = l.Details
                })
                .ToList();

            return logs;
        }

        #region Helpers

        private static string? GetField(Dictionary<string, string> dict, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (dict.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                    return val.Trim();
            }
            return null;
        }

        private static List<Dictionary<string, string>> ParseXlsx(byte[] bytes)
        {
            var list = new List<Dictionary<string, string>>();
            using var ms = new MemoryStream(bytes);
            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws == null) return list;

            var firstRow = ws.FirstRowUsed();
            if (firstRow == null) return list;

            var headers = new List<string>();
            foreach (var cell in firstRow.CellsUsed())
            {
                headers.Add(NormalizeKey(cell.GetString()));
            }

            foreach (var row in ws.RowsUsed().Skip(1))
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                bool hasAnyData = false;

                for (int i = 0; i < headers.Count; i++)
                {
                    var cell = row.Cell(i + 1);
                    var val = cell.Value.ToString();
                    dict[headers[i]] = val;
                    if (!string.IsNullOrWhiteSpace(val)) hasAnyData = true;
                }

                if (hasAnyData) list.Add(dict);
            }

            return list;
        }

        private static List<Dictionary<string, string>> ParseCsv(byte[] bytes)
        {
            var list = new List<Dictionary<string, string>>();
            var text = Encoding.UTF8.GetString(bytes);
            using var reader = new StringReader(text);

            var headerLine = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(headerLine)) return list;

            var headers = SplitCsvLine(headerLine).Select(NormalizeKey).ToList();

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var values = SplitCsvLine(line);
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                bool hasAny = false;

                for (int i = 0; i < headers.Count; i++)
                {
                    var val = i < values.Count ? values[i] : "";
                    dict[headers[i]] = val;
                    if (!string.IsNullOrWhiteSpace(val)) hasAny = true;
                }

                if (hasAny) list.Add(dict);
            }

            return list;
        }

        private static List<string> SplitCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var sb = new StringBuilder();

            foreach (char c in line)
            {
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString().Trim());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            result.Add(sb.ToString().Trim());
            return result;
        }

        private static string NormalizeKey(string key)
        {
            return new string(key.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        }

        private static string EscapeCsv(string? val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            if (val.Contains(",") || val.Contains("\"") || val.Contains("\n"))
            {
                return $"\"{val.Replace("\"", "\"\"")}\"";
            }
            return val;
        }

        private class ParsedProductRowPayload
        {
            public int RowNumber { get; set; }
            public int? ExistingProductId { get; set; }
            public string? Barcode { get; set; }
            public string? Code { get; set; }
            public string? Description { get; set; }
            public decimal? Price { get; set; }
            public decimal? Cost { get; set; }
            public string? CategoryName { get; set; }
            public string? ProductTypeName { get; set; }
            public int? MinimumQuantity { get; set; }
            public string Action { get; set; } = string.Empty;
        }

        #endregion
    }
}
