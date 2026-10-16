using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DoomRu.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;

        public LoginModel(SignInManager<IdentityUser> sm) => _signInManager = sm;

        [BindProperty] public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [Display(Name = "Номер телефона")]
            public string Phone { get; set; } = "";

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string Password { get; set; } = "";

            [Display(Name = "Запомнить меня")]
            public bool RememberMe { get; set; }
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var result = await _signInManager.PasswordSignInAsync(
                Input.Phone, Input.Password, Input.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
                return RedirectToPage("/Index");

            ModelState.AddModelError(string.Empty, "Неверный номер телефона или пароль");
            return Page();
        }
    }
}