using System;
using Elcop.TI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elcop.TI.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260825174328_AdicionarVersaoConcorrenciaAtivo")]
public partial class AdicionarVersaoConcorrenciaAtivo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "VersaoConcorrencia",
            table: "Ativos",
            type: "TEXT",
            nullable: false,
            defaultValue: Guid.Empty);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "VersaoConcorrencia", table: "Ativos");
}
