using OrderAccumulator.Domain.Ordens;
using OrderAccumulator.Domain.Ordens.Strategies;

namespace OrderAccumulator.Tests.Domain.Ordens;

public class LadoStrategyTests
{
	private readonly LadoStrategyFactory _factory =
		new([new CompraStrategy(), new VendaStrategy()]);

	[Fact]
	public void Compra_DeveAumentarExposicao()
	{
		var ordem = OrdemBuilder.Criar(Ativo.VALE3, Lado.Compra, 100, 10m);

		Assert.Equal(1_000m, _factory.Obter(Lado.Compra).CalcularImpacto(ordem));
	}

	[Fact]
	public void Venda_DeveDiminuirExposicao()
	{
		var ordem = OrdemBuilder.Criar(Ativo.VALE3, Lado.Venda, 100, 10m);

		Assert.Equal(-1_000m, _factory.Obter(Lado.Venda).CalcularImpacto(ordem));
	}

	[Theory]
	[InlineData(Lado.Compra, typeof(CompraStrategy))]
	[InlineData(Lado.Venda, typeof(VendaStrategy))]
	public void Factory_DeveResolverStrategyPeloLado(Lado lado, Type esperado)
	{
		Assert.IsType(esperado, _factory.Obter(lado));
	}

	[Fact]
	public void Factory_SemStrategyRegistrada_DeveLancarExcecao()
	{
		var factory = new LadoStrategyFactory([new CompraStrategy()]);

		Assert.Throws<InvalidOperationException>(() => factory.Obter(Lado.Venda));
	}

	[Fact]
	public void Factory_ComStrategyDuplicada_DeveFalharNaCriacao()
	{
		Assert.Throws<ArgumentException>(() =>
			new LadoStrategyFactory([new CompraStrategy(), new CompraStrategy()]));
	}
}