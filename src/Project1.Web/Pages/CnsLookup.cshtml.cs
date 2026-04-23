using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Project1.Web.Services;
using System.Collections.Generic;

namespace Project1.Web.Pages
{
    public class CnsLookupModel : PageModel
    {
        private readonly ICnsMappingService _svc;

        public CnsLookupModel(ICnsMappingService svc)
        {
            _svc = svc;
        }

        [BindProperty]
        public string? Input { get; set; }

        public string? UnicodeResult { get; set; }
        public IEnumerable<string>? CnsResults { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPostLookupByCns()
        {
            if (string.IsNullOrWhiteSpace(Input))
            {
                ModelState.AddModelError("", "請輸入 CNS 編碼或 Unicode 字元。");
                return Page();
            }

            if (_svc.TryGetUnicode(Input!.Trim(), out var ch))
            {
                UnicodeResult = ch;
            }
            else
            {
                ModelState.AddModelError("", "找不到對應的 Unicode 字元。請檢查 CNS 格式。例: 1-4E00 或 A1A1");
            }

            return Page();
        }

        public IActionResult OnPostLookupByChar()
        {
            if (string.IsNullOrWhiteSpace(Input))
            {
                ModelState.AddModelError("", "請輸入 CNS 編碼或 Unicode 字元。");
                return Page();
            }

            var ch = Input!.Trim();
            CnsResults = _svc.GetCnsCodes(ch);
            if (CnsResults == null || !System.Linq.Enumerable.Any(CnsResults))
            {
                ModelState.AddModelError("", "找不到對應的 CNS 編碼。請輸入一個單一字元。很可能檔案未正確解析。");
            }

            return Page();
        }
    }
}
