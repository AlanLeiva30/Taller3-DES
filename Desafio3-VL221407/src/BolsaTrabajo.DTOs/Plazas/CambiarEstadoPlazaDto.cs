using System.ComponentModel.DataAnnotations;
using BolsaTrabajo.Entities.Enums;

namespace BolsaTrabajo.DTOs.Plazas;

/// <summary>Nuevo estado de una plaza: Abierta, EnEvaluacion o Cerrada.</summary>
public class CambiarEstadoPlazaDto
{
    [Required(ErrorMessage = "Seleccione un estado.")]
    [Display(Name = "Estado")]
    public EstadoPlaza Estado { get; set; }
}
