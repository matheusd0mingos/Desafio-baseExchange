using OrderAccumulator.Domain.Ordens;
using OrderAccumulator.Domain.Ordens.Strategies;
using OrderAccumulator.Infrastructure.Persistence;
using OrderAccumulator.Tests.Domain;

namespace OrderAccumulator.Tests.Infrastructure;

public class ExposicaoEmMemoriaRepositoryTests
{
    private readonly ExposicaoEmMemoriaRepository _repositorio = new();

    [Fact]
    public async Task AtivoSemHistorico_DeveVoltarZerado()
    {
        var exposicao = await _repositorio.ObterAsync(Ativo.VIIA4);

        Assert.Equal(0m, exposicao.Valor);
    }

    [Fact]
    public async Task Salvar_DevePersistirOSaldo()
    {
        var exposicao = await _repositorio.ObterAsync(Ativo.PETR4);
        exposicao.Registrar(OrdemBuilder.Criar(Ativo.PETR4, Lado.Compra, 100, 10m), new CompraStrategy());

        await _repositorio.SalvarAsync(exposicao);

        Assert.Equal(1_000m, (await _repositorio.ObterAsync(Ativo.PETR4)).Valor);
    }

    [Fact]
    public async Task MexerNaCopia_SemSalvar_NaoAlteraOOficial()
    {
        var copia = await _repositorio.ObterAsync(Ativo.PETR4);
        copia.Registrar(OrdemBuilder.Criar(Ativo.PETR4, Lado.Compra, 100, 10m), new CompraStrategy());

        var oficial = await _repositorio.ObterAsync(Ativo.PETR4);

        Assert.Equal(1_000m, copia.Valor);
        Assert.Equal(0m, oficial.Valor);
    }
}