using System.ComponentModel.DataAnnotations;

namespace BolsaTrabajo.Entities.Enums;

/// <summary>Etapas por las que pasa una plaza de trabajo.</summary>
public enum EstadoPlaza
{
    [Display(Name = "Abierta")]
    Abierta = 1,

    [Display(Name = "En evaluación")]
    EnEvaluacion = 2,

    [Display(Name = "Cerrada")]
    Cerrada = 3
}
