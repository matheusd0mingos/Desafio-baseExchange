using OrderAccumulator.Domain.Ordens;

namespace OrderAccumulator.Application.Consultas;

public sealed record ExposicaoResumo(Ativo Ativo, decimal Valor);

public sealed record OrdemAceitaResumo(
	Guid OrdemId, Ativo Ativo, Lado Lado, int Quantidade, decimal Preco,
	decimal ExposicaoResultante, DateTimeOffset OcorridaEm);

public sealed record MensagemOutboxResumo(
	Guid Id, string Tipo, string Chave, DateTimeOffset CriadaEm, DateTimeOffset? PublicadaEm);