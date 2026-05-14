using CampusLostAndFound.API.Data;
using CampusLostAndFound.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System;

namespace CampusLostAndFound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ItemsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Item>>> GetItems()
        {
            // DEĞİŞİKLİK: 1 yıl filtresini kaldırdık. Teslim edilmemiş TÜM eşyalar listede görünecek.
            return await _context.Items
                .Where(i => i.IsHandedOver == false)
                .ToListAsync();
        }

        [HttpGet("statistics/{adminLocation}")]
        public async Task<IActionResult> GetStatistics(string adminLocation)
        {
            var locationItems = _context.Items.Where(i => i.Category == adminLocation);
            var oneYearAgo = DateTime.Now.AddYears(-1);

            var totalActive = await locationItems.CountAsync(i => i.IsHandedOver == false && i.CreatedDate > oneYearAgo);
            var totalDelivered = await locationItems.CountAsync(i => i.IsHandedOver == true);

            // 1 yıldan eski olup teslim edilmeyen (Arşivlik/Satışlık) eşya sayısı
            var totalArchived = await locationItems.CountAsync(i => i.IsHandedOver == false && i.CreatedDate <= oneYearAgo);

            var topLocations = await locationItems
                .GroupBy(i => i.Location)
                .Select(g => new { Location = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(3)
                .ToListAsync();

            return Ok(new
            {
                TotalActiveItems = totalActive,
                TotalDeliveredItems = totalDelivered,
                TotalArchivedItems = totalArchived,
                FrequentLocations = topLocations
            });
        }

        // --- TEST İÇİN GEÇİCİ ZAMAN MAKİNESİ ---
        [HttpGet("time-machine")]
        public async Task<IActionResult> TimeMachine()
        {
            var items = await _context.Items.Where(i => i.IsHandedOver == false).Take(3).ToListAsync();

            if (items.Count < 3)
                return BadRequest("Zaman makinesinin çalışması için sistemde en az 3 aktif eşya olmalı.");

            // 1. Eşya: 4 Aylık (TURUNCU)
            items[0].CreatedDate = DateTime.Now.AddDays(-120);

            // 2. Eşya: 7 Aylık (KIRMIZI)
            items[1].CreatedDate = DateTime.Now.AddDays(-210);

            // 3. Eşya: 13 Aylık (KOYU KIRMIZI ve UYARI METNİ ile görünecek)
            items[2].CreatedDate = DateTime.Now.AddDays(-400);

            await _context.SaveChangesAsync();
            return Ok("Zaman yolculuğu başarılı! Lütfen mobil uygulamayı kontrol edin.");
        }

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Dosya seçilmedi.");

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { fileName });
        }

        [HttpPost]
        public async Task<ActionResult<Item>> PostItem(Item item)
        {
            item.CreatedDate = DateTime.Now;
            _context.Items.Add(item);
            await _context.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null) return NotFound();

            try
            {
                item.IsHandedOver = true;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpGet("proxy/{fileName}")]
        public IActionResult GetImage(string fileName)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var imageStream = System.IO.File.OpenRead(filePath);
            string contentType = fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";

            return File(imageStream, contentType);
        }
    }
}