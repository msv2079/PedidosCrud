using System.ComponentModel.DataAnnotations;

namespace PedidosCrud.Web.Models;

public sealed class LoginInput
{
    [Required(ErrorMessage = "Informe o usuário.")]
    [StringLength(100, ErrorMessage = "O usuário deve ter até 100 caracteres.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(256, ErrorMessage = "A senha deve ter até 256 caracteres.")]
    public string Password { get; set; } = string.Empty;
}
