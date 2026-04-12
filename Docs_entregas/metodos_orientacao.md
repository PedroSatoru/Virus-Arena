# Planejamento de Orientação ao Jogador: Virus Arena

Este documento detalha as estratégias de design para guiar o jogador através das mecânicas de *Virus Arena*, utilizando conceitos de orientação direta e indireta conforme as diretrizes pedagógicas de game design.

---

## 1. Métodos de Orientação Direta
*Na orientação direta, o jogador tem consciência de que o jogo está fornecendo instruções ou comandos.*

### **A. Instruções Contextuais (Pop-ups de Imediatismo)**
* **Aplicação:** No início do primeiro nível (**Pulmão**), assim que o primeiro "Atirador Amarelo" realiza o disparo, o jogo exibe um texto breve ou ícone centralizado: *"Bloqueie o tiro amarelo com seu corpo para proteger o órgão!"*.
* **Justificativa:** Aplica o conceito de **imediatismo**. Em vez de um manual longo antes de começar, a informação é dada no exato momento da necessidade, reduzindo a carga cognitiva e garantindo que a mecânica central de *body block* seja compreendida.

### **B. Chamada para Ação (Metas de Curto/Médio Prazo)**
* **Aplicação:** No início de cada órgão, um diálogo rápido via interface (rádio do sistema imunológico) estabelece o objetivo imediato: *"Sobreviva por 3 minutos e mantenha a integridade deste órgão acima de 20% para evitar sequelas!"*.
* **Justificativa:** Transforma o objetivo abstrato de "vencer o jogo" em uma meta mensurável e clara. Isso orienta o esforço do jogador para o gerenciamento do tempo e da barra de vida do cenário.

---

## 2. Métodos de Orientação Indireta
*Na orientação indireta, o design guia o jogador de forma sutil, preservando a sensação de autonomia e descoberta.*

### **A. Design Visual (Luz, Cor e Contraste)**
* **Aplicação:** Projéteis do tipo **Anti-Corpo** (prioridade de defesa) possuem um brilho intenso (Glow) e rastro de luz amarela, contrastando com o fundo avermelhado dos tecidos orgânicos. Já os projéteis **Anti-Player** utilizam tons de roxo opaco.
* **Justificativa:** Utiliza a hierarquia visual para guiar o foco. O olho humano é naturalmente atraído por elementos mais brilhantes e saturados, fazendo com que o jogador priorize instintivamente a interceptação dos tiros que destroem o cenário sem a necessidade de setas indicativas.

### **B. Áudio Espacial e Sonoplastia (Sinalização Fora de Campo)**
* **Aplicação:** Cada tipo de ameaça possui uma assinatura sonora única. O inimigo **Kamikaze**, por exemplo, emite um som de assobio descendente (estilo "mergulho de bomba") assim que inicia o ataque, mesmo que esteja fora do campo de visão imediato do jogador.
* **Justificativa:** Guia o jogador através do som, permitindo que ele tome decisões espaciais (virar para a esquerda ou direita) baseado em estímulos auditivos. Isso mantém o jogador orientado sobre perigos periféricos sem poluir a tela com indicadores visuais excessivos.
