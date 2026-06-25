// URL base da API — sobrescrita em produção via atributo data-api no <script>
// ou definindo window.BARUCHGEST_API antes deste arquivo ser carregado.
(function () {
  if (!window.BARUCHGEST_API) {
    // Em desenvolvimento usa localhost; em produção este arquivo
    // é substituído pelo build ou a variável é injetada pelo servidor.
    window.BARUCHGEST_API = 'http://localhost:5000';
  }
})();
