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

---

## 3. Protótipos Visuais
*As imagens abaixo conectam as decisões de orientação com a interface e as arenas planejadas para o jogo.*

### **A. Tela Inicial**
A tela inicial foi desenhada para ser simples e direta, com opções claras de navegação. O objetivo é reduzir fricção logo no primeiro contato e destacar o fluxo principal de início, tutorial, créditos e saída.

![Tela inicial do jogo](prototipos/menu.jpeg)

### **B. Personagem Jogável**
O protótipo do jogador reforça a leitura rápida da silhueta e facilita a identificação em combate. A forma simples ajuda a destacar movimento, disparo e a função de bloqueio corporal.

![Protótipo do jogador](prototipos/player.jpeg)

### **C. HUD e Leitura de Combate**
O protótipo de gameplay mostra como a interface organiza informação vital: vidas no canto inferior, fase e cronômetro no centro inferior e barra de saúde do corpo no canto oposto. Esse arranjo prioriza decisões rápidas durante a ação.

![Protótipo de gameplay com HUD](prototipos/gameplay.jpg)

### **D. Fase 1 - Pulmão**
Na primeira fase, o cenário introduz a lógica base do jogo: arena horizontal, vida do corpo, inimigos com tiros amarelos e roxos e tempo de sobrevivência de 3 minutos. É a fase de aprendizado da defesa do órgão.

![Protótipo da fase 1 - Pulmão](prototipos/fase1.jpeg)

### **E. Fase 2 - Coração**
A segunda fase amplia a complexidade com mais verticalidade e espaço de deslocamento. O layout reforça o uso de plataformas e a leitura de ameaças laterais, mantendo a barra de vida do corpo como referência estratégica.

![Protótipo da fase 2 - Coração](prototipos/fase2.jpeg)

### **F. Fase 3 - Cérebro**
A terceira fase consolida a defesa total da arena, com ameaças em múltiplas alturas e uso de plataformas móveis. O destaque visual do feixe amarelo ajuda a treinar a identificação do projétil que precisa ser bloqueado pelo jogador.

![Protótipo da fase 3 - Cérebro](prototipos/fase3.jpeg)

### **G. Menu de Pausa**
O menu de pausa mantém o fluxo simples e funcional, permitindo interromper a partida sem quebrar a leitura do jogo. Ele serve como ponto de descanso e reorganização de estratégia.

![Protótipo do menu de pausa](prototipos/pause.jpeg)

### **H. Sistema de Upgrades de DNA**
O menu de upgrades apresenta claramente as mutações disponíveis entre as fases. As opções reforçam o estilo de progressão do jogo: tiro triplo, aumento de velocidade e vida extra.

![Protótipo do menu de upgrades](prototipos/upgrades.jpeg)

### **I. Inimigo: Atirador Anti-Corpo**
Esse inimigo representa a ameaça voltada ao cenário. O uso de cor amarela e visual agressivo ajuda o jogador a associar rapidamente o tiro à necessidade de defesa do órgão.

![Protótipo do Atirador Anti-Corpo](prototipos/atiradorAntiCorpo.jpeg)

### **J. Inimigo: Atirador Anti-Player**
O atirador roxo comunica o foco direto no jogador e diferencia a ameaça daquelas que atacam o ambiente. A cor mais escura ajuda a criar contraste com o tiro amarelo e com o fundo da arena.

![Protótipo do Atirador Anti-Player](prototipos/atiradorAntiPlayer.jpeg)

### **K. Inimigo: Kamikaze**
O kamikaze foi desenhado como uma ameaça de impacto direto, visualmente distinta dos atiradores. A leitura simples reforça a sensação de perigo iminente e combina com a proposta de som de aviso antes da colisão.

![Protótipo do Kamikaze](prototipos/kamikaze.jpeg)

---

## 4. Síntese de Design
Os protótipos reforçam a proposta central de *Virus Arena*: informar o jogador sem excessos, priorizando sinais visuais e decisões rápidas. A progressão das fases, o código de cores dos inimigos e a disposição do HUD trabalham juntos para ensinar o jogador enquanto ele joga.
