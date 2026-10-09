using BolsaTrabajo.Web.Infraestructura;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolsaTrabajo.Web.Controllers;

/// <summary>Envía a cada usuario al panel que corresponde a su rol.</summary>
[Authorize]
public class PanelController : Controller
{
    public IActionResult Index()
    {
        if (User.EsAdministrador())
            return RedirectToAction("Index", "Admin");
        if (User.EsAgente())
            return RedirectToAction("Index", "Agente");
        if (User.EsCandidato())
            return RedirectToAction("Index", "Candidato");

        return RedirectToAction("AccesoDenegado", "Cuenta");
    }
}
