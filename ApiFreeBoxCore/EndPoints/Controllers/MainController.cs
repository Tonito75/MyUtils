namespace EndPoints.Controllers
{
    [Route("/")]
    [ApiController]
    public class MainController : Controller
    {
        private readonly IFreeBoxClient _freeBoxClient;

        public MainController(IFreeBoxClient freeBoxClient)
        {
            _freeBoxClient = freeBoxClient;
        }

        [HttpGet]
        public async Task<ActionResult<LanDevice>> Index()
        {
            var (success, error, result) = await _freeBoxClient.GetConnectedDevicesAsync();
            if (!success)
            {
                return StatusCode(500, $"Impossible de récupérer les appareils connnectés : {error}");
            }

            return Ok(Utils.ParseFreeBoxDeviceToMachines(result));
        }
    }
}
