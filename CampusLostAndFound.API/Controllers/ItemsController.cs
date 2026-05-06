using CampusLostAndFound.API.Data;
using CampusLostAndFound.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;

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

        // 1. Tüm eşyaları listeleme komutu (Müşteriye menüyü verme)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Item>>> GetItems()
        {
            return await _context.Items.ToListAsync();
        }

        // 2. Yeni eşya ekleme komutu (Yeni sipariş alma)
        [HttpPost]
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Dosya seçilmedi.");

            // Dosya adını benzersiz yapalım (Örn: guid_resim.jpg)
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Geriye sadece dosya adını döndürüyoruz
            return Ok(new { fileName });
        }
        public async Task<ActionResult<Item>> PostItem(Item item)
        {
            // Tarihi otomatik şu anki zaman yapalım
            item.CreatedDate = DateTime.Now;

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            return Ok(item);
        }
        // 3. Eşyayı teslim etme (Sistemden silme) komutu
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}