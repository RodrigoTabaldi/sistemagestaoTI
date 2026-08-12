/* ==========================================================================
   ELCOP · Gestão de TI — comportamento da interface
   Sem dependências externas: cada bloco se auto-inicializa a partir de
   atributos data-* no HTML, o que mantém as views declarativas.
   ========================================================================== */
(function () {
    "use strict";

    const Elcop = {};
    window.Elcop = Elcop;

    const $ = (seletor, raiz = document) => raiz.querySelector(seletor);
    const $$ = (seletor, raiz = document) => Array.from(raiz.querySelectorAll(seletor));

    /* ------------------------------------------------------------------ Tema */

    const CHAVE_TEMA = "elcop.tema";
    const CHAVE_SIDEBAR = "elcop.sidebar";

    function aplicarTema(tema) {
        document.documentElement.setAttribute("data-tema", tema);
        localStorage.setItem(CHAVE_TEMA, tema);

        $$("[data-tema-icone]").forEach(el => {
            el.classList.toggle("oculto", el.dataset.temaIcone !== tema);
        });
    }

    Elcop.alternarTema = function () {
        const atual = document.documentElement.getAttribute("data-tema") === "escuro" ? "claro" : "escuro";
        aplicarTema(atual);
    };

    // Aplicado o quanto antes para não piscar a tela clara antes do CSS.
    aplicarTema(localStorage.getItem(CHAVE_TEMA)
        || (window.matchMedia("(prefers-color-scheme: dark)").matches ? "escuro" : "claro"));

    /* --------------------------------------------------------------- Sidebar */

    function iniciarSidebar() {
        const app = $(".app");
        if (!app) return;

        if (localStorage.getItem(CHAVE_SIDEBAR) === "recolhido") app.classList.add("recolhido");

        $$("[data-acao='recolher-menu']").forEach(botao => botao.addEventListener("click", () => {
            app.classList.toggle("recolhido");
            localStorage.setItem(CHAVE_SIDEBAR, app.classList.contains("recolhido") ? "recolhido" : "expandido");
        }));

        $$("[data-acao='abrir-menu']").forEach(botao => botao.addEventListener("click", () => {
            app.classList.add("menu-aberto");
            const cobertura = document.createElement("div");
            cobertura.className = "cobertura-menu";
            cobertura.addEventListener("click", () => {
                app.classList.remove("menu-aberto");
                cobertura.remove();
            });
            document.body.appendChild(cobertura);
        }));
    }

    /* ---------------------------------------------------------------- Toasts */

    const ICONES_TOAST = {
        sucesso: "M20 6L9 17l-5-5",
        erro: "M18 6L6 18M6 6l12 12",
        aviso: "M12 9v4m0 4h.01M10.3 3.9L1.8 18a2 2 0 001.7 3h17a2 2 0 001.7-3L14.7 3.9a2 2 0 00-3.4 0z",
        info: "M12 16v-4m0-4h.01M22 12a10 10 0 11-20 0 10 10 0 0120 0z"
    };

    Elcop.notificar = function (mensagem, tipo = "info", titulo = null) {
        let area = $(".toasts");
        if (!area) {
            area = document.createElement("div");
            area.className = "toasts";
            area.setAttribute("aria-live", "polite");
            document.body.appendChild(area);
        }

        const toast = document.createElement("div");
        toast.className = `toast toast--${tipo}`;
        toast.setAttribute("role", "status");
        toast.innerHTML = `
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2"
                 stroke-linecap="round" stroke-linejoin="round"><path d="${ICONES_TOAST[tipo] || ICONES_TOAST.info}"/></svg>
            <div class="crescer">
                ${titulo ? `<strong></strong>` : ""}
                <p></p>
            </div>
            <button type="button" aria-label="Fechar">&times;</button>`;

        // Conteúdo via textContent: mensagens podem conter dados do usuário.
        if (titulo) $("strong", toast).textContent = titulo;
        $("p", toast).textContent = mensagem;

        const fechar = () => {
            toast.classList.add("saindo");
            setTimeout(() => toast.remove(), 280);
        };

        $("button", toast).addEventListener("click", fechar);
        area.appendChild(toast);
        setTimeout(fechar, 6000);
    };

    function iniciarToastsDoServidor() {
        $$("[data-toast]").forEach(el => {
            Elcop.notificar(el.dataset.toastMensagem, el.dataset.toast, el.dataset.toastTitulo || null);
            el.remove();
        });
    }

    /* ------------------------------------------------------------ Campo de foto */

    const TAMANHO_MAXIMO_FOTO = 4 * 1024 * 1024;

    function iniciarCampoFoto() {
        const campo = $("[data-campo-foto]");
        if (!campo) return;

        const previa = $("[data-previa-foto]");
        const dica = $("[data-nome-foto]");

        campo.addEventListener("change", () => {
            const arquivo = campo.files && campo.files[0];
            if (!arquivo) return;

            // O servidor valida de novo; aqui é só para o usuário não esperar o post.
            if (arquivo.size > TAMANHO_MAXIMO_FOTO) {
                Elcop.notificar("A imagem deve ter no máximo 4 MB.", "erro");
                campo.value = "";
                return;
            }

            if (dica) dica.textContent = arquivo.name;

            if (previa) {
                const url = URL.createObjectURL(arquivo);
                previa.innerHTML = "";

                const img = document.createElement("img");
                img.alt = "Pré-visualização";
                img.src = url;
                img.addEventListener("load", () => URL.revokeObjectURL(url), { once: true });

                previa.appendChild(img);
            }
        });
    }

    /* ------------------------------------------------------------ Confirmação */

    function iniciarConfirmacoes() {
        document.addEventListener("submit", evento => {
            const formulario = evento.target;
            const mensagem = formulario.dataset.confirmar;
            if (!mensagem || formulario.dataset.confirmado === "1") return;

            evento.preventDefault();
            Elcop.confirmar({
                titulo: formulario.dataset.confirmarTitulo || "Confirmar operação",
                mensagem,
                rotuloOk: formulario.dataset.confirmarOk || "Confirmar",
                perigo: formulario.dataset.confirmarPerigo === "true"
            }).then(ok => {
                if (!ok) return;
                formulario.dataset.confirmado = "1";
                formulario.requestSubmit();
            });
        });
    }

    Elcop.confirmar = function ({ titulo, mensagem, rotuloOk = "Confirmar", perigo = false }) {
        return new Promise(resolver => {
            const fundo = document.createElement("div");
            fundo.className = "modal-fundo";
            fundo.innerHTML = `
                <div class="modal" role="dialog" aria-modal="true">
                    <div class="modal__cabecalho"><h3></h3></div>
                    <div class="modal__corpo"><p class="texto-suave" style="margin:0"></p></div>
                    <div class="modal__rodape">
                        <button type="button" class="btn btn--contorno" data-resultado="0">Cancelar</button>
                        <button type="button" class="btn ${perigo ? "btn--perigo" : "btn--primario"}" data-resultado="1"></button>
                    </div>
                </div>`;

            $("h3", fundo).textContent = titulo;
            $("p", fundo).textContent = mensagem;
            $("[data-resultado='1']", fundo).textContent = rotuloOk;

            const encerrar = valor => { fundo.remove(); document.removeEventListener("keydown", aoTeclar); resolver(valor); };
            const aoTeclar = e => { if (e.key === "Escape") encerrar(false); };

            fundo.addEventListener("click", e => {
                if (e.target === fundo) return encerrar(false);
                const botao = e.target.closest("[data-resultado]");
                if (botao) encerrar(botao.dataset.resultado === "1");
            });

            document.addEventListener("keydown", aoTeclar);
            document.body.appendChild(fundo);
            $("[data-resultado='1']", fundo).focus();
        });
    };

    /* --------------------------------------------------------------- Suspenso */

    function iniciarSuspensos() {
        document.addEventListener("click", evento => {
            const gatilho = evento.target.closest("[data-suspenso]");

            $$(".suspenso__conteudo").forEach(painel => {
                const ehDoGatilho = gatilho && painel === $(`#${CSS.escape(gatilho.dataset.suspenso)}`);
                if (!ehDoGatilho) painel.hidden = true;
            });

            if (gatilho) {
                const painel = $(`#${CSS.escape(gatilho.dataset.suspenso)}`);
                if (painel) painel.hidden = !painel.hidden;
            }
        });

        document.addEventListener("keydown", e => {
            if (e.key === "Escape") $$(".suspenso__conteudo").forEach(p => p.hidden = true);
        });
    }

    /* ------------------------------------------------------------------ Abas */

    function iniciarAbas() {
        $$("[data-abas]").forEach(grupo => {
            const abas = $$("[data-aba]", grupo);

            abas.forEach(aba => aba.addEventListener("click", () => {
                abas.forEach(a => a.classList.toggle("ativa", a === aba));
                $$(`[data-painel]`).forEach(painel => {
                    if (painel.dataset.grupo === grupo.dataset.abas)
                        painel.hidden = painel.dataset.painel !== aba.dataset.aba;
                });

                const url = new URL(window.location);
                url.searchParams.set(grupo.dataset.abasParametro || "aba", aba.dataset.aba);
                history.replaceState(null, "", url);
            }));
        });
    }

    /* ------------------------------------------------------------ Filtros */

    function iniciarFiltros() {
        $$("[data-acao='alternar-filtros']").forEach(botao => botao.addEventListener("click", () => {
            const painel = $(`#${CSS.escape(botao.dataset.alvo)}`);
            if (painel) painel.hidden = !painel.hidden;
        }));

        // Selects de filtro submetem o formulário automaticamente.
        $$("[data-filtro-auto] select, [data-filtro-auto] input[type=checkbox]").forEach(campo => {
            campo.addEventListener("change", () => campo.form?.requestSubmit());
        });

        // Busca com atraso, para não disparar a cada tecla.
        $$("[data-busca-instantanea]").forEach(campo => {
            let temporizador;
            campo.addEventListener("input", () => {
                clearTimeout(temporizador);
                temporizador = setTimeout(() => campo.form?.requestSubmit(), 520);
            });
        });
    }

    /* ------------------------------------------------------------ Formulários */

    function iniciarFormularios() {
        document.addEventListener("submit", evento => {
            const formulario = evento.target;
            if (formulario.dataset.confirmar && formulario.dataset.confirmado !== "1") return;
            if (formulario.noValidate === false && !formulario.checkValidity()) return;

            $$("button[type=submit]", formulario).forEach(botao => {
                if (botao.dataset.semBloqueio === "true") return;
                botao.disabled = true;
                botao.dataset.rotuloOriginal = botao.innerHTML;
                botao.innerHTML = `<svg class="girando" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                    stroke-width="2.4" stroke-linecap="round"><path d="M21 12a9 9 0 11-6.2-8.6"/></svg> Salvando...`;
            });
        });

        // Rolagem até o primeiro campo inválido quando o servidor recusa o formulário.
        const primeiroErro = $(".input-validation-error, .validation-summary-errors");
        if (primeiroErro) primeiroErro.scrollIntoView({ behavior: "smooth", block: "center" });
    }

    /* ------------------------------------------------- Verificação remota */

    /**
     * Campos com data-verificar consultam o servidor ao perder o foco para
     * avisar sobre duplicidade antes do envio (o servidor revalida de qualquer forma).
     */
    function iniciarVerificacaoRemota() {
        $$("[data-verificar]").forEach(campo => {
            const alvo = campo.closest(".campo")?.querySelector("[data-verificar-mensagem]");
            if (!alvo) return;

            campo.addEventListener("blur", async () => {
                alvo.textContent = "";
                const valor = campo.value.trim();
                if (!valor) return;

                const id = $("input[name$='.Id'], input[name='Id']")?.value || "0";
                const url = `${campo.dataset.verificar}?${campo.dataset.verificarCampo}=${encodeURIComponent(valor)}&id=${encodeURIComponent(id)}`;

                try {
                    const resposta = await fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } });
                    if (!resposta.ok) return;

                    const resultado = await resposta.json();
                    if (typeof resultado === "string") {
                        alvo.textContent = resultado;
                        campo.classList.add("input-validation-error");
                    } else {
                        campo.classList.remove("input-validation-error");
                    }
                } catch {
                    // Falha de rede não bloqueia o preenchimento: o servidor valida no envio.
                }
            });
        });
    }

    /* --------------------------------------------------------------- Máscaras */

    const MASCARAS = {
        cpf: v => v.replace(/\D/g, "").slice(0, 11)
            .replace(/(\d{3})(\d)/, "$1.$2").replace(/(\d{3})(\d)/, "$1.$2").replace(/(\d{3})(\d{1,2})$/, "$1-$2"),
        cnpj: v => v.replace(/\D/g, "").slice(0, 14)
            .replace(/^(\d{2})(\d)/, "$1.$2").replace(/^(\d{2})\.(\d{3})(\d)/, "$1.$2.$3")
            .replace(/\.(\d{3})(\d)/, ".$1/$2").replace(/(\d{4})(\d)/, "$1-$2"),
        telefone: v => {
            const digitos = v.replace(/\D/g, "").slice(0, 11);
            if (digitos.length <= 10) return digitos.replace(/(\d{2})(\d)/, "($1) $2").replace(/(\d{4})(\d)/, "$1-$2");
            return digitos.replace(/(\d{2})(\d)/, "($1) $2").replace(/(\d{5})(\d)/, "$1-$2");
        },
        imei: v => v.replace(/\D/g, "").slice(0, 17),
        numero: v => v.replace(/\D/g, "")
    };

    function iniciarMascaras() {
        $$("[data-mascara]").forEach(campo => {
            const mascara = MASCARAS[campo.dataset.mascara];
            if (!mascara) return;

            const aplicar = () => { campo.value = mascara(campo.value); };
            campo.addEventListener("input", aplicar);
            if (campo.value) aplicar();
        });
    }

    /* ------------------------------------------------------------- Números */

    /* Sem contagem progressiva: o número é um dado, não um efeito. Quem está
       conferindo estoque quer ler o valor, não esperar ele subir na tela. */
    function escreverNumero(elemento) {
        const valor = parseFloat(elemento.dataset.contador);
        if (Number.isNaN(valor)) return;

        const decimais = parseInt(elemento.dataset.contadorDecimais || "0", 10);
        const prefixo = elemento.dataset.contadorPrefixo || "";
        const sufixo = elemento.dataset.contadorSufixo || "";

        elemento.textContent = prefixo + valor.toLocaleString("pt-BR", {
            minimumFractionDigits: decimais, maximumFractionDigits: decimais
        }) + sufixo;
    }

    /* -------------------------------------------- Preenchimento dos gráficos */

    function iniciarRevelacao() {
        // Números saem prontos no carregamento.
        $$("[data-contador]").forEach(escreverNumero);

        // Barras e roscas precisam do valor aplicado via style/atributo; o
        // IntersectionObserver evita calcular gráfico que ainda está fora da tela.
        const alvos = $$("[data-barra], [data-rosca-valor]");
        if (!alvos.length) return;

        if (!("IntersectionObserver" in window)) {
            alvos.forEach(revelar);
            return;
        }

        const observador = new IntersectionObserver(entradas => {
            entradas.forEach(entrada => {
                if (!entrada.isIntersecting) return;
                revelar(entrada.target);
                observador.unobserve(entrada.target);
            });
        }, { threshold: .25 });

        alvos.forEach(alvo => observador.observe(alvo));
    }

    function revelar(elemento) {
        if (elemento.hasAttribute("data-barra")) {
            elemento.style.width = `${elemento.dataset.barra}%`;
            return;
        }

        if (elemento.hasAttribute("data-rosca-valor")) {
            const circunferencia = 2 * Math.PI * Number(elemento.getAttribute("r"));
            const fatia = circunferencia * Number(elemento.dataset.roscaValor);
            elemento.setAttribute("stroke-dasharray", `${fatia} ${circunferencia}`);
        }
    }

    /* ----------------------------------------------------------------- Kanban */

    function iniciarKanban() {
        const quadro = $("[data-kanban]");
        if (!quadro) return;

        const url = quadro.dataset.kanban;
        const token = $("input[name='__RequestVerificationToken']", quadro)?.value;
        let arrastado = null;

        $$(".kanban__cartao", quadro).forEach(cartao => {
            cartao.draggable = true;

            cartao.addEventListener("dragstart", evento => {
                arrastado = cartao;
                cartao.classList.add("arrastando");
                evento.dataTransfer.effectAllowed = "move";
                evento.dataTransfer.setData("text/plain", cartao.dataset.id);
            });

            cartao.addEventListener("dragend", () => {
                cartao.classList.remove("arrastando");
                arrastado = null;
                $$(".kanban__coluna", quadro).forEach(c => c.classList.remove("recebendo"));
            });
        });

        $$(".kanban__coluna", quadro).forEach(coluna => {
            const lista = $(".kanban__lista", coluna);

            coluna.addEventListener("dragover", evento => {
                evento.preventDefault();
                evento.dataTransfer.dropEffect = "move";
                coluna.classList.add("recebendo");

                const referencia = posicaoDeInsercao(lista, evento.clientY);
                if (!arrastado) return;
                referencia ? lista.insertBefore(arrastado, referencia) : lista.appendChild(arrastado);
            });

            coluna.addEventListener("dragleave", evento => {
                if (!coluna.contains(evento.relatedTarget)) coluna.classList.remove("recebendo");
            });

            coluna.addEventListener("drop", async evento => {
                evento.preventDefault();
                coluna.classList.remove("recebendo");
                if (!arrastado) return;

                const cartao = arrastado;
                const statusAnterior = cartao.dataset.status;
                const novoStatus = coluna.dataset.status;
                const ordem = Array.from(lista.children).indexOf(cartao) + 1;

                cartao.dataset.status = novoStatus;
                atualizarContadores(quadro);

                const dados = new FormData();
                dados.append("id", cartao.dataset.id);
                dados.append("status", novoStatus);
                dados.append("ordem", ordem);
                if (token) dados.append("__RequestVerificationToken", token);

                try {
                    const resposta = await fetch(url, {
                        method: "POST",
                        body: dados,
                        headers: { "X-Requested-With": "XMLHttpRequest" }
                    });

                    if (!resposta.ok) throw new Error(await extrairMensagem(resposta));

                    if (statusAnterior !== novoStatus)
                        Elcop.notificar(`"${cartao.dataset.titulo}" movida para ${coluna.dataset.rotulo}.`, "sucesso");
                } catch (erro) {
                    Elcop.notificar(erro.message || "Não foi possível mover a demanda.", "erro");
                    setTimeout(() => window.location.reload(), 1200);
                }
            });
        });

        function posicaoDeInsercao(lista, y) {
            const candidatos = $$(".kanban__cartao:not(.arrastando)", lista);

            return candidatos.reduce((maisProximo, cartao) => {
                const caixa = cartao.getBoundingClientRect();
                const deslocamento = y - caixa.top - caixa.height / 2;

                return deslocamento < 0 && deslocamento > maisProximo.deslocamento
                    ? { deslocamento, elemento: cartao }
                    : maisProximo;
            }, { deslocamento: Number.NEGATIVE_INFINITY, elemento: null }).elemento;
        }

        function atualizarContadores(raiz) {
            $$(".kanban__coluna", raiz).forEach(coluna => {
                const total = $$(".kanban__cartao", coluna).length;
                const contador = $("[data-contador-coluna]", coluna);
                if (contador) contador.textContent = total;
            });
        }
    }

    async function extrairMensagem(resposta) {
        try {
            const corpo = await resposta.json();
            return corpo.mensagem;
        } catch {
            return null;
        }
    }

    /* --------------------------------------------- Pré-visualização de entrega */

    function iniciarEntrega() {
        const tela = $("[data-entrega]");
        if (!tela) return;

        const ativos = JSON.parse($("#catalogo-ativos")?.textContent || "[]");
        const pessoas = JSON.parse($("#catalogo-colaboradores")?.textContent || "[]");

        const seletorAtivo = $("[data-entrega-ativo]");
        const seletorPessoa = $("[data-entrega-colaborador]");
        const painelAtivo = $("[data-preview-ativo]");
        const painelPessoa = $("[data-preview-colaborador]");
        const campoAcessorios = $("[data-entrega-acessorios]");

        function desenharAtivo() {
            const item = ativos.find(a => String(a.id) === seletorAtivo.value);
            if (!item) { painelAtivo.innerHTML = vazio("Selecione um ativo para ver os detalhes."); return; }

            painelAtivo.innerHTML = `
                <div class="linha g-3 mb-3">
                    <span class="icone-tipo"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor"
                        stroke-width="1.9"><use href="#i-${escapar(item.icone)}"></use></svg></span>
                    <div class="crescer">
                        <strong class="texto-forte">${escapar(item.descricao)}</strong>
                        <div class="pequeno texto-suave">${escapar(item.tipo)} · ${escapar(item.patrimonio)}</div>
                    </div>
                </div>
                <dl class="lista-dados">
                    ${linha("Nº de série", item.serie)}
                    ${linha("IMEI", item.imei)}
                    ${linha("Linha", item.linha)}
                    ${linha("Condição", item.condicao)}
                    ${linha("Acessórios", item.acessorios)}
                </dl>`;

            if (campoAcessorios && !campoAcessorios.value && item.acessorios)
                campoAcessorios.value = item.acessorios;
        }

        function desenharPessoa() {
            const pessoa = pessoas.find(p => String(p.id) === seletorPessoa.value);
            if (!pessoa) { painelPessoa.innerHTML = vazio("Selecione quem está retirando o equipamento."); return; }

            painelPessoa.innerHTML = `
                <div class="linha g-3 mb-3">
                    <span class="avatar" style="background:${corDoNome(pessoa.nome)}">${escapar(pessoa.iniciais)}</span>
                    <div class="crescer">
                        <strong class="texto-forte">${escapar(pessoa.nome)}</strong>
                        <div class="pequeno texto-suave">${escapar(pessoa.cargo || "—")}</div>
                    </div>
                </div>
                <dl class="lista-dados">
                    ${linha("Matrícula", pessoa.matricula)}
                    ${linha("E-mail", pessoa.email)}
                    ${linha("Departamento", pessoa.departamento)}
                    ${linha("Ativos em posse", `${pessoa.ativosEmPosse}`)}
                </dl>`;
        }

        const escapar = texto => String(texto ?? "").replace(/[&<>"']/g,
            c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);

        const linha = (rotulo, valor) =>
            valor ? `<dt>${rotulo}</dt><dd>${escapar(valor)}</dd>` : "";

        const vazio = texto => `<p class="texto-tenue pequeno" style="margin:0">${texto}</p>`;

        const corDoNome = nome => {
            const cores = ["#8B1F26", "#3F6FB5", "#2E9E6B", "#B98A2E", "#9C2B7A", "#C4741C", "#4A5568"];
            const soma = [...String(nome)].reduce((total, c) => total + c.charCodeAt(0), 0);
            return cores[soma % cores.length];
        };

        seletorAtivo?.addEventListener("change", desenharAtivo);
        seletorPessoa?.addEventListener("change", desenharPessoa);
        desenharAtivo();
        desenharPessoa();
    }

    /* ------------------------------------------------------------ Impressão */

    function iniciarImpressao() {
        $$("[data-acao='imprimir']").forEach(botao =>
            botao.addEventListener("click", () => window.print()));
    }

    /* ------------------------------------------------------- Atalhos globais */

    function iniciarAtalhos() {
        document.addEventListener("keydown", evento => {
            const digitando = /^(INPUT|TEXTAREA|SELECT)$/.test(document.activeElement?.tagName);

            // "/" foca a busca da tela atual.
            if (evento.key === "/" && !digitando) {
                const busca = $("[data-busca-instantanea], .busca .entrada");
                if (busca) { evento.preventDefault(); busca.focus(); busca.select(); }
            }

            // Ctrl+K alterna o tema.
            if (evento.key.toLowerCase() === "k" && (evento.ctrlKey || evento.metaKey)) {
                evento.preventDefault();
                Elcop.alternarTema();
            }
        });
    }

    /* -------------------------------------------------------------- Boot */

    document.addEventListener("DOMContentLoaded", () => {
        iniciarSidebar();
        iniciarToastsDoServidor();
        iniciarCampoFoto();
        iniciarConfirmacoes();
        iniciarSuspensos();
        iniciarAbas();
        iniciarFiltros();
        iniciarFormularios();
        iniciarVerificacaoRemota();
        iniciarMascaras();
        iniciarRevelacao();
        iniciarKanban();
        iniciarEntrega();
        iniciarImpressao();
        iniciarAtalhos();

        $$("[data-acao='alternar-tema']").forEach(botao =>
            botao.addEventListener("click", Elcop.alternarTema));
    });
})();
