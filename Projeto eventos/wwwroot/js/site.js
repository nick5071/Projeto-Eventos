
window.cpfValido = false;

function validarCPF(cpf) {
    cpf = cpf.replace(/\D/g, "");

    if (cpf.length !== 11) return false;

    if (/^(\d)\1{10}$/.test(cpf)) return false;

    let soma = 0;
    for (let i = 0; i < 9; i++) {
        soma += parseInt(cpf[i]) * (10 - i);
    }

    let resto = (soma * 10) % 11;
    if (resto === 10) resto = 0;
    if (resto !== parseInt(cpf[9])) return false;

    soma = 0;
    for (let i = 0; i < 10; i++) {
        soma += parseInt(cpf[i]) * (11 - i);
    }

    resto = (soma * 10) % 11;
    if (resto === 10) resto = 0;

    return resto === parseInt(cpf[10]);
}

document.addEventListener("DOMContentLoaded", () => {
    const cpfInput = document.getElementById("cpf");
    const erroCPF = document.getElementById("erroCPF");

    if (!cpfInput) return;

    cpfInput.addEventListener("input", () => {
        cpfInput.value = cpfInput.value.replace(/\D/g, "");

        if (cpfInput.value.length === 11) {
            if (validarCPF(cpfInput.value)) {
                erroCPF.classList.remove("ativo");
                cpfInput.classList.remove("border-erro");
                window.cpfValido = true;
            } else {
                erroCPF.classList.add("ativo");
                cpfInput.classList.add("border-erro");
                window.cpfValido = false;
            }
        } else {
            erroCPF.classList.remove("ativo");
            cpfInput.classList.remove("border-erro");
            window.cpfValido = false;
        }
    });
});
