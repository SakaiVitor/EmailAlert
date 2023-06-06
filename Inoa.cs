using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

class Tunel
{
    public string Ticker { get; set; }
    public float LimiteSuperior { get; set; }
    public float LimiteInferior { get; set; }
    public float? ValorAcao { get; set; }
    private bool valorAcaoUltrapassouLimiteSuperior;
    private bool valorAcaoUltrapassouLimiteInferior;

    public Tunel(string ticker, float limiteSuperior, float limiteInferior)
    {
        Ticker = ticker;
        LimiteSuperior = limiteSuperior;
        LimiteInferior = limiteInferior;
        ValorAcao = null; // Valor inicial nulo
    }

    private ConsoleColor ObterCorValorAcao()
    {
        if (ValorAcao.HasValue)
        {
            if (ValorAcao.Value > LimiteSuperior)
            {
                return ConsoleColor.Green; // Valor acima do limite superior (verde)
            }
            else if (ValorAcao.Value < LimiteInferior)
            {
                return ConsoleColor.Red; // Valor abaixo do limite inferior (vermelho)
            }
        }

        return ConsoleColor.White; // Valor dentro dos limites (branco)
    }

    public void MostrarDetalhes(DateTime ultimaAtualizacao)
    {
    Console.WriteLine("Ticker: " + Ticker);
    Console.WriteLine("Limite Superior: " + LimiteSuperior.ToString("F2", CultureInfo.InvariantCulture));
    Console.WriteLine("Limite Inferior: " + LimiteInferior.ToString("F2", CultureInfo.InvariantCulture));

    // Obter a cor correspondente ao valor da ação
    ConsoleColor corValorAcao = ObterCorValorAcao();
    
    Console.ForegroundColor = corValorAcao; // Definir cor para o valor da ação
    Console.WriteLine("Valor da Ação: " + (ValorAcao.HasValue ? ValorAcao.Value.ToString("F2", CultureInfo.InvariantCulture) : "Valor ainda não foi atualizado"));
    Console.ResetColor(); // Resetar cor do console

    Console.WriteLine("Última atualização: " + ultimaAtualizacao.ToString("yyyy-MM-dd HH:mm:ss"));
    }
}

class SentinelaEmail
{
    private string emailDestino;
    private string host;
    private int porta;
    private string usuario;
    private string senha;

    public SentinelaEmail(IConfiguration configuration)
    {
        var config = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .Build();

        emailDestino = config["EmailDestino"];
        host = config["SmtpHost"];
        porta = int.Parse(config["SmtpPorta"]);
        usuario = config["SmtpUsuario"];
        senha = config["SmtpSenha"];
    }

    public async Task EnviarAlerta(float? valorAcao, string limiteUltrapassado)
    {
        string mensagemTexto = $"O valor da ação ultrapassou o limite {limiteUltrapassado}. Valor atual: {valorAcao:F2}";

        MailMessage mensagem = new MailMessage(usuario, emailDestino, "Alerta de valor da ação", mensagemTexto);

        using (SmtpClient smtpClient = new SmtpClient(host, porta))
        {
            smtpClient.Credentials = new System.Net.NetworkCredential(usuario, senha);
            smtpClient.EnableSsl = true;

            await smtpClient.SendMailAsync(mensagem);
        }
    }
}

class Program
{
    static Tunel? tunel;
    static HttpClient? httpClient;
    static DateTime ultimaAtualizacao = DateTime.MinValue;
    static SentinelaEmail sentinelaEmail;
    static bool valorAcaoUltrapassouLimiteSuperior = false;
    static bool valorAcaoUltrapassouLimiteInferior = false;
    
    static async Task Main(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        if (args.Length != 3)
        {
            Console.WriteLine("Uso: programa.exe ticker limiteSuperior limiteInferior");
            return;
        }

        string ticker = args[0];
        float limiteSuperior = float.Parse(args[1], CultureInfo.InvariantCulture);
        float limiteInferior = float.Parse(args[2], CultureInfo.InvariantCulture);

        tunel = new Tunel(ticker, limiteSuperior, limiteInferior);

        // Exibe os detalhes iniciais do túnel
        ultimaAtualizacao = DateTime.MinValue;
        tunel.MostrarDetalhes(ultimaAtualizacao);

        // Configuração do HttpClient
        httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("https://brapi.dev/api/");

        // Inicializa a SentinelaEmail
        sentinelaEmail = new SentinelaEmail(configuration);

        // Inicia a busca do valor da ação
        await BuscarValorAcao();

        // Loop principal
        while (true)
        {
            // Limpa a tela e posiciona o cursor no topo
            Console.Clear();
            Console.SetCursorPosition(0, 0);

            // Exibe os detalhes atualizados do túnel
            tunel.MostrarDetalhes(ultimaAtualizacao);

            // Aguarda 10 segundos
            Thread.Sleep(10000);

            // Busca o valor da ação novamente
            await BuscarValorAcao();
        }
    }

    static async Task BuscarValorAcao()
    {
        try
        {
            string ticker = tunel.Ticker;

            // Busca o preço atual da ação usando o ticker
            string url = $"quote/{ticker}?range=1d&interval=1d&fundamental=false&dividends=false";

            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();

            dynamic data = JsonConvert.DeserializeObject(responseBody);
            float precoAtual = data.results[0].regularMarketPrice;

            bool valorAcaoAcimaLimiteSuperior = tunel.ValorAcao > tunel.LimiteSuperior;
            bool valorAcaoAbaixoLimiteInferior = tunel.ValorAcao < tunel.LimiteInferior;

            if (valorAcaoAcimaLimiteSuperior)
            {
                valorAcaoUltrapassouLimiteSuperior = true;
            }
            else if (valorAcaoAbaixoLimiteInferior)
            {
                valorAcaoUltrapassouLimiteInferior = true;
            }

            // Atualiza o valor da ação no objeto Tunel
            tunel.ValorAcao = precoAtual;

            // Atualiza a data e hora da última atualização
            ultimaAtualizacao = DateTime.Now;

            // Verifica se o valor da ação ultrapassou os limites
            if (valorAcaoAcimaLimiteSuperior && !valorAcaoUltrapassouLimiteSuperior)
            {
                await sentinelaEmail.EnviarAlerta(tunel.ValorAcao, $"superior ({tunel.LimiteSuperior})");
                valorAcaoUltrapassouLimiteSuperior = true;
            }
            else if (valorAcaoAbaixoLimiteInferior && !valorAcaoUltrapassouLimiteInferior)
            {
                await sentinelaEmail.EnviarAlerta(tunel.ValorAcao, $"inferior ({tunel.LimiteInferior})");
                valorAcaoUltrapassouLimiteInferior = true;
            }

            Console.WriteLine("Valor da Ação: " + (tunel.ValorAcao?.ToString("F2", CultureInfo.InvariantCulture) ?? "Valor ainda não foi atualizado"));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao buscar o valor da ação: " + ex.Message);
        }
    }
}
