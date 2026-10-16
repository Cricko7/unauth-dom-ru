using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DoomRu.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;

        public LoginModel(SignInManager<IdentityUser> sm)
        {
            _signInManager = sm;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Введите номер телефона")]
            [Display(Name = "Номер телефона")]
            public string Phone { get; set; } = "";

            [Required(ErrorMessage = "Введите пароль")]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string Password { get; set; } = "";

            [Display(Name = "Запомнить меня")]
            public bool RememberMe { get; set; }
        }

        public void OnGet(string? returnUrl = null, string? phone = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                ModelState.AddModelError(string.Empty, ErrorMessage);

            // если номер пришёл из query (?phone=+79001234567) — подставляем
            if (!string.IsNullOrEmpty(phone))
                Input.Phone = phone;

            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
                return Page();

            // ---- нормализация номера ----
            var digits = new string((Input.Phone ?? "").Where(char.IsDigit).ToArray());
            if (digits.StartsWith("8")) digits = "7" + digits.Substring(1);
            if (!digits.StartsWith("7")) digits = "7" + digits;

            if (digits.Length != 11)
            {
                ModelState.AddModelError(nameof(Input.Phone), "Формат: +79001234567");
                return Page();
            }

            var normalizedPhone = "+" + digits;

            var result = await _signInManager.PasswordSignInAsync(
                normalizedPhone,
                Input.Password,
                Input.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
                return LocalRedirect(returnUrl);

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Аккаунт заблокирован");
                return Page();
            }

            ModelState.AddModelError(string.Empty, "Неверный номер телефона или пароль");
            return Page();
        }
    }
}
