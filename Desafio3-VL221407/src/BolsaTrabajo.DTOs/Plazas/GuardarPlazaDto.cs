using System.ComponentModel.DataAnnotations;

namespace BolsaTrabajo.DTOs.Plazas;

/// <summary>Datos que el agente escribe al crear o editar una plaza.</summary>
public class GuardarPlazaDto
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(150, ErrorMessage = "El título no puede superar 150 caracteres.")]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(4000)]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de publicación es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de publicación")]
    public DateTime FechaPublicacion { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "La fecha de cierre es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de cierre")]
    public DateTime FechaCierre { get; set; } = DateTime.Today.AddDays(15);

    [Required(ErrorMessage = "La institución es obligatoria.")]
    [StringLength(150)]
    [Display(Name = "Institución")]
    public string Institucion { get; set; } = string.Empty;
}
