# GDD: Virus Arena - Documento de Design

# 1. Página de Título

**Título do Jogo:** Virus Arena

---

### **Sistemas de Jogo Previstos**
**Shooter Tower Defense 2D:** O jogador assume o papel de um glóbulo branco em uma arena fixa. O jogo combina a movimentação ágil e o combate de tiro lateral (estilo Metal Slug) com a estratégia de defesa de território, onde o objetivo é impedir que vírus invasores destruam os órgãos vitais que compõem o cenário.

---

### **Público-Alvo e Classificação**
* **Faixa Etária Recomendada:** 10+ anos.
* **Classificação Indicativa ESRB Pretendida:** E10+ (Everyone 10+).
---

### **Data de Lançamento Prevista**
* **Conclusão do Projeto:** 08/05/2026.

# 2. ESBOÇO DO JOGO

### **Resumo da História**
O jogo narra a batalha microscópica pela sobrevivência humana. O jogador é um **Glóbulo Branco**, a última linha de defesa do organismo. A missão é proteger órgãos vitais de uma invasão viral. A jornada começa nos **Pulmões**, avança pelo **Coração** e culmina no **Cérebro**. O destino do hospedeiro humano depende da eficiência do jogador em conter os danos em cada área.

### **Ângulo de Câmera e Localização**
* **Câmera:** Visão 2D lateral fixa em formato de arena quadrada (estilo *Single-Screen Shooter*).
* **Localização:** O interior do corpo humano. O cenário é estático em cada fase, representando o tecido do órgão atual (Pulmão, Coração e Cérebro).

### **Fluxo do Jogo e Progressão**
* **Estrutura de Níveis:** O jogo possui 3 níveis principais com 2 transições de cenário.
* **Sistema de Power-ups:** Ao vencer um nível, o jogador deve escolher **um entre três** aprimoramentos para a próxima etapa:
    1. **Tiro Triplo:** Aumenta a área de cobertura do ataque.
    2. **Mais Velocidade:** Melhora a agilidade de movimentação e salto.
    3. **Resistência Celular:** Adiciona +1 coração ao jogador e recupera 30% da vida total do corpo.
* **Condição de Vitória:** Sobreviver às hordas nos três órgãos. A vitória final é alcançada se a vida total do corpo permanecer acima de zero ao fim da fase do Cérebro.

### **Mecânicas de Dano e Multiplicadores**
O cenário (corpo) possui **1500 de vida total**, mas cada órgão tem uma tolerância e um impacto diferente na saúde geral:
* **Dano Local:** Se um órgão sofrer dano excessivo (ex: 500 pontos), o jogador "perde" aquela fase, mas o jogo continua para a próxima, com consequências na história.
* **Multiplicadores de Impacto:** O dano ao corpo é relativo à importância do órgão:
    * **Pulmão (Nível 1):** Multiplicador x1 (10 de dano no cenário = 10 no corpo).
    * **Coração (Nível 2):** Multiplicador x2 (10 de dano no cenário = 20 no corpo).
    * **Cérebro (Nível 3):** Multiplicador x3 (10 de dano no cenário = 30 no corpo).

### **Sistema de Desfecho (Finais Alternativos)**
O final do jogo é determinado pelo estado de preservação dos órgãos e do corpo:
* **Final Perfeito:** Todos os órgãos defendidos com sucesso; o humano sobrevive saudável.
* **Final com Sequelas:** Se o jogador perdeu a vida de um cenário específico (ex: Pulmão), a cutscene final mostra o humano vivo, mas com problemas crônicos naquele órgão.
* **Final de Falência Sistêmica:** Se a vida total do corpo (1500) chegar a zero em qualquer momento, o humano morre e o jogo termina em *Game Over*.
