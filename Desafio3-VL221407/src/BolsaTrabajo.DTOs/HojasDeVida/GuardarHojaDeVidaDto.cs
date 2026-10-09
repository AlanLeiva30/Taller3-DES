using System.ComponentModel.DataAnnotations;

namespace BolsaTrabajo.DTOs.HojasDeVida;

/// <summary>Datos que el candidato escribe al crear o actualizar su hoja de vida.</summary>
public class GuardarHojaDeVidaDto
{
    [Required(ErrorMessage = "Indique su formación académica.")]
    [StringLength(4000, ErrorMessage = "La formación académica no puede superar 4000 caracteres.")]
    [Display(Name = "Formación académica")]
    public string FormacionAcademica { get; set; } = string.Empty;

    [StringLength(4000, ErrorMessage = "La experiencia laboral no puede superar 4000 caracteres.")]
    [Display(Name = "Experiencia laboral")]
    public string ExperienciaLaboral { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Las competencias no pueden superar 2000 caracteres.")]
    [Display(Name = "Competencias")]
    public string Competencias { get; set; } = string.Empty;
}
