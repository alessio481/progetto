using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManager.Migrations
{
    /// <inheritdoc />
    public partial class PrimoDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DashboardSnapshots",
                columns: table => new
                {
                    DashboardSnapshotID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Giorno = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VeicoliTotali = table.Column<int>(type: "int", nullable: false),
                    VeicoliDisponibili = table.Column<int>(type: "int", nullable: false),
                    VeicoliInUso = table.Column<int>(type: "int", nullable: false),
                    VeicoliInManutenzione = table.Column<int>(type: "int", nullable: false),
                    SegnalazioniAperte = table.Column<int>(type: "int", nullable: false),
                    PrenotazioniAttive = table.Column<int>(type: "int", nullable: false),
                    ManutenzioniAperte = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardSnapshots", x => x.DashboardSnapshotID);
                });

            migrationBuilder.CreateTable(
                name: "Utenti",
                columns: table => new
                {
                    UtenteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cognome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataNascita = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ruolo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataRegistrazione = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utenti", x => x.UtenteID);
                });

            migrationBuilder.CreateTable(
                name: "Veicoli",
                columns: table => new
                {
                    VeicoloId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Targa = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Modello = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Stato = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LivelloCarburante = table.Column<int>(type: "int", nullable: false),
                    Colore = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Carburante = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cilindrata = table.Column<int>(type: "int", nullable: true),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAggiornamento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UtentePrenotatoID = table.Column<int>(type: "int", nullable: true),
                    UtenteManutentoreID = table.Column<int>(type: "int", nullable: true),
                    PostoAuto = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veicoli", x => x.VeicoloId);
                    table.ForeignKey(
                        name: "FK_Veicoli_Utenti_UtenteManutentoreID",
                        column: x => x.UtenteManutentoreID,
                        principalTable: "Utenti",
                        principalColumn: "UtenteID");
                    table.ForeignKey(
                        name: "FK_Veicoli_Utenti_UtentePrenotatoID",
                        column: x => x.UtentePrenotatoID,
                        principalTable: "Utenti",
                        principalColumn: "UtenteID");
                });

            migrationBuilder.CreateTable(
                name: "Manutenzioni",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VeicoloId = table.Column<int>(type: "int", nullable: false),
                    UtenteId = table.Column<int>(type: "int", nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataInizio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFine = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manutenzioni", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Manutenzioni_Utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Utenti",
                        principalColumn: "UtenteID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Manutenzioni_Veicoli_VeicoloId",
                        column: x => x.VeicoloId,
                        principalTable: "Veicoli",
                        principalColumn: "VeicoloId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Prenotazioni",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VeicoloId = table.Column<int>(type: "int", nullable: false),
                    UtenteId = table.Column<int>(type: "int", nullable: false),
                    OraPrenotazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OraRilascio = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prenotazioni", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prenotazioni_Utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Utenti",
                        principalColumn: "UtenteID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Prenotazioni_Veicoli_VeicoloId",
                        column: x => x.VeicoloId,
                        principalTable: "Veicoli",
                        principalColumn: "VeicoloId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Segnalazioni",
                columns: table => new
                {
                    SegnalazioneID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtenteID = table.Column<int>(type: "int", nullable: false),
                    VeicoloID = table.Column<int>(type: "int", nullable: true),
                    PostoID = table.Column<int>(type: "int", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Stato = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Segnalazioni", x => x.SegnalazioneID);
                    table.ForeignKey(
                        name: "FK_Segnalazioni_Utenti_UtenteID",
                        column: x => x.UtenteID,
                        principalTable: "Utenti",
                        principalColumn: "UtenteID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Segnalazioni_Veicoli_VeicoloID",
                        column: x => x.VeicoloID,
                        principalTable: "Veicoli",
                        principalColumn: "VeicoloId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Manutenzioni_UtenteId",
                table: "Manutenzioni",
                column: "UtenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Manutenzioni_VeicoloId",
                table: "Manutenzioni",
                column: "VeicoloId");

            migrationBuilder.CreateIndex(
                name: "IX_Prenotazioni_UtenteId",
                table: "Prenotazioni",
                column: "UtenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Prenotazioni_VeicoloId",
                table: "Prenotazioni",
                column: "VeicoloId");

            migrationBuilder.CreateIndex(
                name: "IX_Segnalazioni_UtenteID",
                table: "Segnalazioni",
                column: "UtenteID");

            migrationBuilder.CreateIndex(
                name: "IX_Segnalazioni_VeicoloID",
                table: "Segnalazioni",
                column: "VeicoloID");

            migrationBuilder.CreateIndex(
                name: "IX_Veicoli_UtenteManutentoreID",
                table: "Veicoli",
                column: "UtenteManutentoreID");

            migrationBuilder.CreateIndex(
                name: "IX_Veicoli_UtentePrenotatoID",
                table: "Veicoli",
                column: "UtentePrenotatoID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardSnapshots");

            migrationBuilder.DropTable(
                name: "Manutenzioni");

            migrationBuilder.DropTable(
                name: "Prenotazioni");

            migrationBuilder.DropTable(
                name: "Segnalazioni");

            migrationBuilder.DropTable(
                name: "Veicoli");

            migrationBuilder.DropTable(
                name: "Utenti");
        }
    }
}
