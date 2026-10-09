using System.ComponentModel.DataAnnotations;

namespace BolsaTrabajo.Entities.Enums;

/// <summary>Resultado de la evaluación de una postulación.</summary>
public enum EstadoPostulacion
{
    [Display(Name = "En revisión")]
    EnRevision = 1,

    [Display(Name = "Aprobada")]
    Aprobada = 2,

    [Display(Name = "Rechazada")]
    Rechazada = 3
}
