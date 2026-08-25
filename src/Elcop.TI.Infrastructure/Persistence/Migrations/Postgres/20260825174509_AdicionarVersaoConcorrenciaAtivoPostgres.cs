using System;
using Elcop.TI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elcop.TI.Infrastructure.Persistence.Migrations.Postgres;

[DbContext(typeof(AppDbContextPostgres))]
[Migration("20260825174509_AdicionarVersaoConcorrenciaAtivoPostgres")]
public partial class AdicionarVersaoConcorrenciaAtivoPostgres : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "VersaoConcorrencia",
            table: "Ativos",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "VersaoConcorrencia", table: "Ativos");
}
