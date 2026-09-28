using OrderAccumulator.Domain.Ordens;

namespace OrderAccumulator.Tests.Domain;

/// <summary>Atalho para criar ordens válidas nos testes.</summary>
internal static class OrdemBuilder
{
	public static Ordem Criar(Ativo ativo, Lado lado, int quantidade, decimal preco) =>
		Ordem.Criar(Guid.NewGuid(), ativo, lado, quantidade, preco).Value;
}