using System.ComponentModel.DataAnnotations;

namespace LibreriaBazar.Models;

public static class Roles
{
    public const string Propietario = "Propietario";
    public const string Encargado = "Encargado";
}

public class Usuario
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string NombreCompleto { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Rol { get; set; } = Roles.Encargado;

    public bool Activo { get; set; } = true;

    // RNF11: bloqueo temporal tras 3 intentos fallidos
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHasta { get; set; }
}
