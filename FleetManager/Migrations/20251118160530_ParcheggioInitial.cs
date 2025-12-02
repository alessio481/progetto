using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManager.Migrations
{
    /// <inheritdoc />
    public partial class ParcheggioInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PostiAuto",
                columns: table => new
                {
                    PostoID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodicePosto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Stato = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostiAuto", x => x.PostoID);
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
                    Targa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Modello = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Stato = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataRegistrazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LivelloCarburante = table.Column<int>(type: "int", nullable: false),
                    PostoID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veicoli", x => x.VeicoloId);
                    table.ForeignKey(
                        name: "FK_Veicoli_PostiAuto_PostoID",
                        column: x => x.PostoID,
                        principalTable: "PostiAuto",
                        principalColumn: "PostoID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Manutenzioni",
                columns: table => new
                {
                    ManutenzioneID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VeicoloID = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Costo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Stato = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manutenzioni", x => x.ManutenzioneID);
                    table.ForeignKey(
                        name: "FK_Manutenzioni_Veicoli_VeicoloID",
                        column: x => x.VeicoloID,
                        principalTable: "Veicoli",
                        principalColumn: "VeicoloId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Prenotazioni",
                columns: table => new
                {
                    PrenotazioneID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtenteID = table.Column<int>(type: "int", nullable: false),
                    VeicoloID = table.Column<int>(type: "int", nullable: false),
                    DataInizioRichiesta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFinePrevista = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Scopo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Stato = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prenotazioni", x => x.PrenotazioneID);
                    table.ForeignKey(
                        name: "FK_Prenotazioni_Utenti_UtenteID",
                        column: x => x.UtenteID,
                        principalTable: "Utenti",
                        principalColumn: "UtenteID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Prenotazioni_Veicoli_VeicoloID",
                        column: x => x.VeicoloID,
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
                    Descrizione = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataSegnalazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Stato = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RispostaAdmin = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Segnalazioni", x => x.SegnalazioneID);
                    table.ForeignKey(
                        name: "FK_Segnalazioni_PostiAuto_PostoID",
                        column: x => x.PostoID,
                        principalTable: "PostiAuto",
                        principalColumn: "PostoID");
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
                name: "IX_Manutenzioni_VeicoloID",
                table: "Manutenzioni",
                column: "VeicoloID");

            migrationBuilder.CreateIndex(
                name: "IX_Prenotazioni_UtenteID",
                table: "Prenotazioni",
                column: "UtenteID");

            migrationBuilder.CreateIndex(
                name: "IX_Prenotazioni_VeicoloID",
                table: "Prenotazioni",
                column: "VeicoloID");

            migrationBuilder.CreateIndex(
                name: "IX_Segnalazioni_PostoID",
                table: "Segnalazioni",
                column: "PostoID");

            migrationBuilder.CreateIndex(
                name: "IX_Segnalazioni_UtenteID",
                table: "Segnalazioni",
                column: "UtenteID");

            migrationBuilder.CreateIndex(
                name: "IX_Segnalazioni_VeicoloID",
                table: "Segnalazioni",
                column: "VeicoloID");

            migrationBuilder.CreateIndex(
                name: "IX_Veicoli_PostoID",
                table: "Veicoli",
                column: "PostoID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Manutenzioni");

            migrationBuilder.DropTable(
                name: "Prenotazioni");

            migrationBuilder.DropTable(
                name: "Segnalazioni");

            migrationBuilder.DropTable(
                name: "Utenti");

            migrationBuilder.DropTable(
                name: "Veicoli");

            migrationBuilder.DropTable(
                name: "PostiAuto");
        }
    }
}
