using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using MercadoPago.Error;
using MercadoPago.Resource.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Projeto_eventos.Models;
using System.Diagnostics;
using System.Text.Json;

namespace Projeto_eventos.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly Conexao _conexao;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private const string FavoritosSessionKey = "Favoritos";
        private readonly IConfiguration _config;

        public HomeController(ILogger<HomeController> logger, Conexao conexao, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IConfiguration config)
        {
            _logger = logger;
            _conexao = conexao;
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
        }

        public IActionResult Index()
        {
            var eventos = _conexao.Eventos
                .Where(e => e.Data >= DateTime.Now)
                .ToList();

            return View(eventos);
        }

        [Authorize]
        public IActionResult BuscarFavoritos(string pesquisa)
        {
            var favoritosIds = GetFavoritos();

            
            var BuscarFavorito = _conexao.Eventos
                .Where(e => favoritosIds.Contains(e.Id) && e.Nome.Contains(pesquisa))
                .ToList();

            if (BuscarFavorito.Any())
            {
                return View("PaginaFavoritos", BuscarFavorito);
            }

            ViewBag.Mensagem = "Nenhum evento favorito encontrado com esse nome.";
            return View("PaginaFavoritos", new List<Evento>());
        }

        private string GetFavoritosKey()
        {
            // Se o usuário estiver logado, cria uma chave específica pra ele
            var userId = _userManager.GetUserId(User);
            return string.IsNullOrEmpty(userId) ? FavoritosSessionKey : $"{FavoritosSessionKey}_{userId}";
        }

        private List<int> GetFavoritos()
        {
            var key = GetFavoritosKey();
            var json = HttpContext.Session.GetString(key);
            return string.IsNullOrEmpty(json)
                ? new List<int>()
                : JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        }

        [Authorize]
        public async Task<IActionResult> MeusIngressos()
        {
            var usuario = await _userManager.GetUserAsync(User);

            var ingressos = _conexao.Ingressos
                .Include(i => i.Evento)
                .Where(i => i.UserId == usuario.Id && (i.Evento.Data >= DateTime.Now.AddDays(-7)))
                .OrderByDescending(i => i.DataCompra)
                .ToList();

            foreach (var ingresso in ingressos)
            {
                if (ingresso.Status == "ativo" && ingresso.Evento.Data < DateTime.Now)
                {
                    ingresso.Status = "expirado";
                }
            }

            await _conexao.SaveChangesAsync();

            return View(ingressos);
        }

        [Authorize]
        private void SaveFavoritos(List<int> favoritos)
        {
            var key = GetFavoritosKey(); 
            var json = JsonSerializer.Serialize(favoritos);
            HttpContext.Session.SetString(key, json); 
        }

        [Authorize]
        public IActionResult PaginaFavoritos()
        {
            var favoritosIds = GetFavoritos();
            var eventos = _conexao.Eventos.Where(e => favoritosIds.Contains(e.Id)).ToList();
            return View(eventos);
        }

        [Authorize]
        public IActionResult Adicionar(int id)
        {
            var favoritos = GetFavoritos();
            if (!favoritos.Contains(id))
            {
                favoritos.Add(id);
                SaveFavoritos(favoritos);
            }
            TempData["Sucesso"] = "Adicionado aos favoritos";
            return RedirectToAction("Index");
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletarConta(string senha)
        {
            var usuario = await _userManager.GetUserAsync(User);

            if (usuario == null)
            {
                TempData["Erro"] = "Usuário não encontrado.";
                return RedirectToAction("Conta");
            }

            // Verifica a senha digitada
            var senhaValida = await _userManager.CheckPasswordAsync(usuario, senha);
            if (!senhaValida)
            {
                TempData["Erro"] = "Senha incorreta. Tente novamente.";
                return RedirectToAction("Conta");
            }

            // Faz logout e deleta
            await _signInManager.SignOutAsync();
            var resultado = await _userManager.DeleteAsync(usuario);

            if (resultado.Succeeded)
            {
                TempData["Sucesso"] = "Conta deletada com sucesso!";
                return RedirectToAction("Index", "Home");
            }

            TempData["Erro"] = "Erro ao deletar a conta.";
            foreach (var erro in resultado.Errors)
            {
                Console.WriteLine(erro.Description);
            }

            return RedirectToAction("Conta");
        }

        public IActionResult Remover(int id)
        {
            var favoritos = GetFavoritos();
            favoritos.Remove(id);
            SaveFavoritos(favoritos);
            return RedirectToAction("PaginaFavoritos");
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Eventos()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Admin()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AdminDeletarUsuario(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Erro = "Digite um email.";
                return View("Admin");
            }

            var usuario = await _userManager.FindByEmailAsync(email);

            if (usuario == null)
            {
                ViewBag.Erro = "Usuário não encontrado.";
                return View("Admin");
            }

            var resultado = await _userManager.DeleteAsync(usuario);

            if (resultado.Succeeded)
            {
                ViewBag.Sucesso = "Usuário deletado com sucesso.";
            }
            else
            {
                ViewBag.Erro = "Erro ao deletar usuário.";
            }

            return View("Admin");
        }

        [HttpPost]
        public async Task<IActionResult> FinalizarPagamento(PagamentoViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var evento = _conexao.Eventos.FirstOrDefault(e => e.Id == model.EventoId);
                    return View("Pagamento", evento);
                }

                var evento1 = _conexao.Eventos.FirstOrDefault(e => e.Id == model.EventoId);
                decimal valorFinal;

                if (model.Nome.Length > 100 || model.CPF.Length > 14 || model.Rua.Length > 100 || model.Cidade.Length > 50)
                {
                    return View();
                }

                if (model.TipoIngresso == "meia")
                    valorFinal = evento1.Valor / 2;
                else
                    valorFinal = evento1.Valor;

                var client = new PaymentClient();

                var paymentRequest = new PaymentCreateRequest
                {
                    TransactionAmount = valorFinal,
                    Token = model.CardToken,
                    Description = $"Ingresso Evento {model.EventoId}",
                    Installments = 1,
                    Payer = new PaymentPayerRequest
                    {
                        Email = "seuemail@gmail.com",
                        Identification = new IdentificationRequest
                        {
                            Type = "CPF",
                            Number = model.CPF
                        }
                    }
                };

                var payment = await client.CreateAsync(paymentRequest);

                if (payment.Status == "approved")
                {
                    var evento = _conexao.Eventos.First(e => e.Id == model.EventoId);

                    if (evento == null)
                    {
                        return NotFound("Evento não encontrado.");
                    }


                    var usuario = await _userManager.GetUserAsync(User);
                    var emailComprador = usuario?.Email ?? "emailteste@gmail.com";

                    var ingresso = new Ingresso
                    {
                        EventoId = evento.Id,
                        UserId = usuario.Id,
                        TipoIngresso = model.TipoIngresso,
                        ValorPago = valorFinal,
                        DataCompra = DateTime.Now
                    };

                    string corpoEmail = $@"
                        <h2>🎟️ Compra confirmada!</h2>
                        <p>Olá {model.Nome} Seu pagamento foi aprovado com sucesso.</p>
                        <hr/>
                        <p><strong>Evento:</strong> {evento.Nome}</p>
                        <p><strong>Data:</strong> {evento.Data:dd/MM/yyyy HH:mm}</p>
                        <p><strong>Local:</strong> {evento.Local}</p>
                        <p><strong>Tipo de ingresso:</strong> {model.TipoIngresso}</p>
                        <p><strong>Valor pago:</strong> R$ {valorFinal:N2}</p>
                        <br/>
                        <p>Apresente este email na entrada do evento.</p>
                        <p><strong>Bom show! 🎶</strong></p>
                        ";

                    EnviarEmail(
                        emailComprador,
                        $"Ingresso confirmado - {evento.Nome}",
                        corpoEmail
                    );

                    var sucessoVm = new SucessoPagamentoViewModel
                    {
                        Evento = evento,
                        ValorFinal = valorFinal,
                        EventoDados = model
                    };
                    
                    _conexao.Ingressos.Add(ingresso);
                    await _conexao.SaveChangesAsync();

                    return View("Sucesso", sucessoVm);
                }
            }
            catch (MercadoPagoApiException)
            {
                ModelState.AddModelError("", "Dados do cartão inválidos.");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Erro inesperado ao processar pagamento.");
            }

            var eventoErro = _conexao.Eventos.First(e => e.Id == model.EventoId);
            return View("Pagamento", eventoErro);
        }



        public IActionResult AcessoNegado()
        {
            return View();
        }

        public IActionResult EsqueciSenha()
        {
            return View();
        }


        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Deletar(int id)
        {
            var ProcurarEvento = _conexao.Eventos.FirstOrDefault(e => e.Id == id);

            if (ProcurarEvento != null)
            {
                _conexao.Eventos.Remove(ProcurarEvento);
                _conexao.SaveChanges();
                return RedirectToAction("Index");
            }

            TempData["MensagemEventoErro"] = "Id não encontrado";
            return RedirectToAction("Index");

        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult EditarBotao(int id, Evento eventos, IFormFile Imagem2)
        {
            var ProcurarEventoEditarB = _conexao.Eventos.FirstOrDefault(b => b.Id == id);

            if (ProcurarEventoEditarB == null)
            {
                TempData["MensagemErro"] = "Evento não encontrado!";
                return RedirectToAction("Index");
            }

            ModelState.Remove("ImagemURL");

            if (!ModelState.IsValid)
            {
                return View("Editar", eventos);
            }

            if (eventos.Data < DateTime.Now)
            {
                ViewBag.MensagemErro = "Não é possível criar um evento no passado!";
                return View("Editar", eventos);
            }

            ProcurarEventoEditarB.Nome = eventos.Nome;
            ProcurarEventoEditarB.Descricao = eventos.Descricao;
            ProcurarEventoEditarB.Data = eventos.Data;
            ProcurarEventoEditarB.Local = eventos.Local;
            ProcurarEventoEditarB.TipoEvento = eventos.TipoEvento;
            ProcurarEventoEditarB.Valor = eventos.Valor;
            // Atualiza imagem apenas se o usuário enviar uma nova
            if (Imagem2 != null && Imagem2.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(Imagem2.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    Imagem2.CopyTo(fileStream);
                }

                ProcurarEventoEditarB.ImagemURL = "/images/" + uniqueFileName;
            }

            _conexao.SaveChanges();
            TempData["MensagemEditar"] = $"Evento {eventos.Nome} editado com sucesso!";
            return RedirectToAction("Index");
        }

        [Authorize]
        public IActionResult Pagamento(int id)
        {
            var evento = _conexao.Eventos.FirstOrDefault(e => e.Id == id);

            if (evento == null)
                return NotFound();

            return View(evento);
        }

        public IActionResult Ingressos(int id)
        {
            var ProcurarEventoIngressos = _conexao.Eventos.FirstOrDefault(a => a.Id == id);
            if (ProcurarEventoIngressos != null)
            {
                return View("PaginaIngresso",ProcurarEventoIngressos);
            }
            TempData["MensagemEventoErro"] = "Id não encontrado";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Editar(int id)
        {
            var ProcurarEventoEditar = _conexao.Eventos.FirstOrDefault(a => a.Id == id);

            if (ProcurarEventoEditar != null)
            {
                return View(ProcurarEventoEditar);
            }

            TempData["MensagemEventoErro"] = "Id não encontrado";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult CriarEvento(Evento eventos, IFormFile Imagem2)
        {
            var VerEventoIgual = _conexao.Eventos.FirstOrDefault(e => e.Nome == eventos.Nome && e.Local == eventos.Local && e.Data == eventos.Data);

            if (VerEventoIgual != null)
            {
                TempData["MensagemErro"] = $"Evento com o nome: {eventos.Nome} já existe! no mesmo Local e mesma Data!";
                return RedirectToAction("Eventos");
            }

            if (eventos.Data < DateTime.Now)
            {
                ViewBag.MensagemErro = "Não é possível criar um evento no passado!";
                return View("Eventos", eventos);
            }

            if (Imagem2 != null && Imagem2.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);


                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(Imagem2.FileName);


                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    Imagem2.CopyTo(fileStream);
                }


                eventos.ImagemURL = "/images/" + uniqueFileName;
            }

            _conexao.Eventos.Add(eventos);
            _conexao.SaveChanges();

            TempData["Mensagem"] = $"Evento {eventos.Nome} foi criado com sucesso!";
            return RedirectToAction("Eventos");
        }

        public IActionResult TodosEventos()
        {
            var eventos = _conexao.Eventos.ToList();
            return View(eventos);
        }

        public IActionResult PesquisarTipo(string categoria, decimal? precoMin, decimal? precoMax)
        {
            var eventos = _conexao.Eventos.AsQueryable();

            if (!string.IsNullOrEmpty(categoria))
            {
                eventos = eventos.Where(e => e.TipoEvento == categoria);
            }

            if (precoMin.HasValue)
            {
                eventos = eventos.Where(e => e.Valor >= precoMin.Value); 
            }

            if (precoMax.HasValue)
            {
                eventos = eventos.Where(e => e.Valor <= precoMax.Value); 
            }

            return View("TodosEventos", eventos.ToList());
        }


        [HttpGet("TodosEventos/Paginado")]
        public IActionResult TodosEventos(int page = 1)
        {
            int pageSize = 6; 

            int totalEventos = _conexao.Eventos.Count();

            var eventosPaginados = _conexao.Eventos
                .OrderBy(e => e.Data) 
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPages = (int)Math.Ceiling(totalEventos / (double)pageSize);
            ViewBag.CurrentPage = page;

            return View(eventosPaginados);
        }

        public IActionResult Registro()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Registrar(Registro usuarios)
        {
            var RegistroExiste = await _userManager.FindByEmailAsync(usuarios.Email);

            if (RegistroExiste != null)
            {
                ViewBag.MensagemErro = "Já existe um usuário com esse e-mail.";
                return View("Registro", usuarios);
            }

            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = usuarios.Email, Email = usuarios.Email };
                var result = await _userManager.CreateAsync(user, usuarios.Password);

                if (result.Succeeded)
                {
                    TempData["MensagemRegistro"] = "Registro realizado com sucesso!";
                    return RedirectToAction("Registro");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(nameof(usuarios.Password), error.Description);
                }
            }

            return View("Registro", usuarios);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AtualizarConta(string nome)
        {
            var usuario = await _userManager.GetUserAsync(User);

            if (usuario == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(nome))
            {
                TempData["Erro"] = "O nome não pode ser vazio.";
                return RedirectToAction("Conta");
            }

            if (nome.Length > 100)
            {
                TempData["Erro"] = "O nome completo deve ter no máximo 100 caracteres.";
                return RedirectToAction("Conta");
            }

            usuario.UserName = nome; 

            var resultado = await _userManager.UpdateAsync(usuario);

            if (resultado.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(usuario);
                TempData["Sucesso"] = "Nome atualizado com sucesso!";
            }
            else
            {
                TempData["Erro"] = string.Join(" ", resultado.Errors.Select(e => e.Description));
            }

            return RedirectToAction("Conta");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Conta()
        {
            // pega o usuário logado
            var usuario = await _userManager.GetUserAsync(User);

            if (usuario == null)
            {
                return RedirectToAction("Login");
            }

            return View(usuario);
        }

        public async Task<IActionResult> Logar(Login model)
        {
            if (ModelState.IsValid)
            {
                var usuario = await _userManager.FindByEmailAsync(model.Email);

                if (usuario == null)
                {
                    ModelState.AddModelError("Email", "E-mail não encontrado.");
                    return View("Login", model);
                }

                var senhaCorreta = await _userManager.CheckPasswordAsync(usuario, model.Password);

                if (!senhaCorreta)
                {
                    ModelState.AddModelError("Password", "Senha incorreta.");
                    return View("Login", model);
                }

                var resultado = await _signInManager.PasswordSignInAsync(
                    usuario.UserName,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (resultado.Succeeded)
                {
                    return RedirectToAction("Index");
                }

                ViewBag.MensagemErro = "Erro ao realizar login.";
            }

            return View("Login", model);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AlterarSenha(string senhaAtual, string novaSenha, string confirmarSenha)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return NotFound();

            if (string.IsNullOrWhiteSpace(senhaAtual) || string.IsNullOrWhiteSpace(novaSenha) || string.IsNullOrWhiteSpace(confirmarSenha))
            {
                TempData["Erro"] = "Preencha todos os campos de senha.";
                return RedirectToAction("Conta");
            }

            if (novaSenha != confirmarSenha)
            {
                TempData["Erro"] = "A nova senha e a confirmação não coincidem.";
                return RedirectToAction("Conta");
            }

            // Troca de senha
            var resultado = await _userManager.ChangePasswordAsync(usuario, senhaAtual, novaSenha);
            if (resultado.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(usuario);
                TempData["Sucesso"] = "Senha atualizada com sucesso!";
            }
            else
            {
                if (resultado.Errors.Any(e => e.Code == "PasswordMismatch"))
                {
                    TempData["Erro"] = "A senha atual está incorreta.";
                }
                else
                {
                    TempData["Erro"] = string.Join(" ", resultado.Errors.Select(e => e.Description));
                }
            }

            return RedirectToAction("Conta");
        }

        private bool EnviarEmail(string destino, string assunto, string corpoHtml)
        {
            try
            {
                var smtpHost = _config["Smtp:Host"];
                var smtpPort = int.Parse(_config["Smtp:Port"]);
                var smtpUser = _config["Smtp:Username"];
                var smtpPass = _config["Smtp:Password"];

                var smtp = new System.Net.Mail.SmtpClient(smtpHost)
                {
                    Port = smtpPort,
                    Credentials = new System.Net.NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                var mensagem = new System.Net.Mail.MailMessage
                {
                    From = new System.Net.Mail.MailAddress(smtpUser, "Projeto Eventos"),
                    Subject = assunto,
                    Body = corpoHtml,
                    IsBodyHtml = true
                };
                mensagem.To.Add(destino);

                smtp.Send(mensagem);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao enviar e-mail: " + ex.Message);
                return false;
            }
        }

        [HttpPost]
        public async Task<IActionResult> EsqueciSenha(EsqueciSenha model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("Email", "E-mail não encontrado.");
                return View(model);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("RedefinirSenha", "Home", new { token, email = user.Email }, Request.Scheme);

            string corpo = $"Clique no link para redefinir sua senha: <a href='{resetLink}'>Redefinir Senha</a>";
            bool enviado = EnviarEmail(user.Email, "Redefinição de senha", corpo);

            ViewBag.Mensagem = enviado ?
                "Verifique seu e-mail para redefinir a senha." :
                "Falha ao enviar o e-mail. Tente novamente mais tarde.";

            return View();
        }

        [HttpGet]
        public IActionResult RedefinirSenha(string token, string email)
        {
            var model = new RedefinirSenha { Token = token, Email = email };
            return View("NovaSenha",model);
        }

        [HttpPost]
        public async Task<IActionResult> RedefinirSenha(RedefinirSenha model)
        {
            if (!ModelState.IsValid)
                return View("NovaSenha", model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ViewBag.MensagemErro = "Usuário não encontrado.";
                return View();
            }

            var resultado = await _userManager.ResetPasswordAsync(user, model.Token, model.NovaSenha);

            if (resultado.Succeeded)
            {
                TempData["Sucesso"] = "Senha redefinida com sucesso! Faça login novamente.";
                return RedirectToAction("Login");
            }

            foreach (var error in resultado.Errors)
            {
                if (error.Code == "InvalidToken")
                {
                    return View("LinkExpirado");
                }

                ModelState.AddModelError("", error.Description);
            }


            return View("NovaSenha", model);
        }

        public IActionResult PesquisarPorNome(string pesquisa)
        {
            var BuscarEventoNome = _conexao.Eventos.Where(p => p.Nome.Contains(pesquisa)).ToList();

            if (BuscarEventoNome.Any()) 
            {
                return View("TodosEventos", BuscarEventoNome);
            }
            ViewBag.Mensagem = "Nenhum evento encontrado com esse nome.";
            return View("TodosEventos", new List<Evento>());
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
