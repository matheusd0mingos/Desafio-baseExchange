using Microsoft.AspNetCore.Mvc;
using OrderAccumulator.Application.Consultas;
using OrderAccumulator.Domain.Exposicoes;
using OrderAccumulator.Domain.Ordens;
using OrderExposure.Contracts.Responses;

namespace OrderAccumulator.Api.Controllers;

/// <summary>Só leitura. Nada aqui altera estado.</summary>
[ApiController]
[Route("api")]
public sealed class ConsultasController(IConsultasOrdens consultas) : ControllerBase
{
    private const int LimitePadrao = 50;
    private const int LimiteMaximo = 200;

    [HttpGet("exposicoes")]
    public async Task<IEnumerable<ExposicaoResponse>> Exposicoes(CancellationToken ct) =>
        (await consultas.ListarExposicoesAsync(ct)).Select(e => new ExposicaoResponse(
            e.Ativo.ToString(),
            e.Valor,
            ExposicaoAtivo.Limite,
            Math.Round(Math.Abs(e.Valor) / ExposicaoAtivo.Limite * 100, 2)));

    [HttpGet("ordens")]
    public async Task<IEnumerable<OrdemAceitaResponse>> Ordens([FromQuery] int limite = LimitePadrao, CancellationToken ct = default) =>
        (await consultas.ListarOrdensAceitasAsync(Limitar(limite), ct)).Select(o => new OrdemAceitaResponse(
            o.OrdemId, o.Ativo.ToString(), o.Lado == Lado.Compra ? "C" : "V",
            o.Quantidade, o.Preco, o.ExposicaoResultante, o.OcorridaEm));

    [HttpGet("outbox")]
    public async Task<IEnumerable<MensagemOutboxResponse>> Outbox([FromQuery] int limite = LimitePadrao, CancellationToken ct = default) =>
        (await consultas.ListarOutboxAsync(Limitar(limite), ct)).Select(m => new MensagemOutboxResponse(
            m.Id, m.Tipo, m.Chave, m.CriadaEm, m.PublicadaEm,
            m.PublicadaEm is null ? "Pendente" : "Publicada"));

    private static int Limitar(int limite) => Math.Clamp(limite, 1, LimiteMaximo);
}