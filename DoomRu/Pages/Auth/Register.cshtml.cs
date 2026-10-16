using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DoomRu.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public RegisterModel(UserManager<IdentityUser> um, SignInManager<IdentityUser> sm)
        {
            _userManager = um;
            _signInManager = sm;
        }

        [BindProperty] public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Введите номер телефона")]
            [Display(Name = "Номер телефона")]
            public string Phone { get; set; } = "";
            [Required(ErrorMessage = "Введите пароль")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Минимум 6 символов")]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string Password { get; set; } = "";

            [DataType(DataType.Password)]
            [Display(Name = "Повтор пароля")]
            [Compare("Password", ErrorMessage = "Пароли не совпадают")]
            public string ConfirmPassword { get; set; } = "";
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            // нормализуем: убираем всё, кроме цифр, приводим к +7XXXXXXXXXX
            var digits = new string(Input.Phone.Where(char.IsDigit).ToArray());
            if (digits.StartsWith("8")) digits = "7" + digits.Substring(1);
            if (!digits.StartsWith("7")) digits = "7" + digits;
            if (digits.Length != 11)
            {
                ModelState.AddModelError(nameof(Input.Phone), "Формат: +79001234567");
                return Page();
            }
            Input.Phone = "+" + digits;

            var user = new IdentityUser
            {
                UserName = Input.Phone,
                PhoneNumber = Input.Phone,
                PhoneNumberConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToPage("/Index");
            }

            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);

            return Page();
        }
    }
}
