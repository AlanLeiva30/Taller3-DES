using System.ComponentModel.DataAnnotations;
using BolsaTrabajo.Entities.Enums;

namespace BolsaTrabajo.DTOs.Postulaciones;

/// <summary>Plaza a la que el candidato desea postularse.</summary>
public class CrearPostulacionDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Indique una plaza válida.")]
    [Display(Name = "Plaza")]
    public int IdPlaza { get; set; }
}

/// <summary>Resultado de la evaluación: Aprobada o Rechazada.</summary>
public class EvaluarPostulacionDto
{
    [Required(ErrorMessage = "Seleccione el resultado de la evaluación.")]
    [Display(Name = "Resultado")]
    public EstadoPostulacion Estado { get; set; }
}
