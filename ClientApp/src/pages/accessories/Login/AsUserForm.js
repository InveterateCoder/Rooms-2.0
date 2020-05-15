import React, { useContext, useState } from "react";
import { Context } from "../../../data/Context";
import { Link } from "react-router-dom";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faSignInAlt, faUserPlus, faKey } from "@fortawesome/free-solid-svg-icons";
import LocalizedStrings from "react-localization";
import validator from "../../../utils/validator";
import { Post } from "../../../utils/requests";
import urls from "../../../utils/Urls";
import { Loading } from "../../../Loading";

const text = new LocalizedStrings({
    en: {
        header: {
            sign: "Sign In",
            register: "Registration",
            recover: "Password recovery"
        },
        inputs: {
            sign: [["Email", "email"], ["Password", "password"]],
            register: [["Email", "email"], ["User name", "name"],
            ["Password", "password"], ["Confirm password", "confirm"]],
            recover: [["Email", "email"]]
        },
        buttons: {
            sign: [["Sign In", "/signin/user/sign", faSignInAlt],
            ["Recover password", "/signin/user/recover", faKey],
            ["Register", "/signin/user/register", faUserPlus]],
            recover: [["Recover password", "/signin/user/recover", faKey],
            ["Sign In", "/signin/user/sign", faSignInAlt]],
            register: [["Register", "/signin/user/register", faUserPlus],
            ["Sign In", "/signin/user/sign", faSignInAlt]]
        },
        regHeader: "The first stage of your registration has been successful.",
        regFirst: "To complete the process please check your email for a validation request. Within the email you will find a link which you must click in order to activate your account. The link will become invalid in one hour.",
        regSecond: "If the email doesn't appear shortly, please be sure to check your spam / junk mail folder. Some anti-spam filters modify the email, so first copy any spam message to your inbox before clicking the link.",
        pswdSent: "Your password has been sent to your email address."
    },
    ru: {
        header: {
            sign: "Вход",
            register: "Регистрация",
            recover: "Восстановление пароля"
        },
        inputs: {
            sign: [["Эл. почта", "email"], ["Пароль", "password"]],
            register: [["Эл. почта", "email"], ["Имя пользователя", "name"],
            ["Пароль", "password"], ["Подтвердите пароль", "confirm"]],
            recover: [["Эл. почта", "email"]]
        },
        buttons: {
            sign: [["Войти", "/signin/user/sign", faSignInAlt],
            ["Восстановить пароль", "/signin/user/recover", faKey],
            ["Зарегистрироваться ", "/signin/user/register", faUserPlus]],
            recover: [["Восстановить пароль", "/signin/user/recover", faKey],
            ["Войти", "/signin/user/sign", faSignInAlt]],
            register: [["Зарегистрироваться", "/signin/user/register", faUserPlus],
            ["Войти", "/signin/user/sign", faSignInAlt]]
        },
        regHeader: "Первый этап вашей регистрации прошел успешно.",
        regFirst: "Чтобы завершить процесс, пожалуйста, проверьте свою электронную почту. В электронном письме вы найдете ссылку, по которой вы должны щелкнуть, чтобы активировать свою учетную запись. Ссылка станет недействительной через час.",
        regSecond: "Если письмо не появится в ближайшее время, обязательно проверьте папку со спамом / нежелательной почтой. Некоторые антиспам-фильтры изменяют электронную почту, поэтому сначала скопируйте спам-сообщение в свой почтовый ящик, прежде чем щелкнуть по ссылку.",
        pswdSent: "Ваш пароль был отправлен на ваш адрес электронной почты."
    }
})

export function AsUserForm(props) {
    const context = useContext(Context);
    const [state, setState] = useState({
        email: "",
        emailError: validator.email("", context.lang),
        name: "",
        nameError: validator.name("", context.lang),
        password: "",
        passwordError: validator.password("", context.lang),
        confirm: "",
        confirmError: validator.confirm("", "", context.lang)
    });
    const [loading, setLoading] = useState(false);
    const [chEmail, setChEmail] = useState(false);
    const [rstPswd, setRstPswd] = useState(false);
    const inputChange = ev => {
        let value = ev.target.value;
        let name = ev.target.name;
        let error = validator[name](value, context.lang);
        if (name === "password" && state.confirm) {
            let confError = validator.confirm(value, state.confirm, context.lang);
            setState({ ...state, password: value, passwordError: error, confirmError: confError });
        } else setState({ ...state, [name]: value, [name + "Error"]: error });
    }
    const confirm = ev => {
        let value = ev.target.value;
        let error = validator.confirm(state.password,
            ev.target.value, context.lang);
        setState({ ...state, confirm: value, confirmError: error });
    }
    const submit = () => {
        let isValid = true;
        switch (props.match.params.action) {
            case "register":
                if (state.nameError || state.confirmError) {
                    isValid = false;
                    break;
                }
            // eslint-disable-next-line
            case "sign":
                if (state.passwordError) {
                    isValid = false;
                    break;
                }
            // eslint-disable-next-line
            case "recover":
                if (state.emailError)
                    isValid = false;
                break;
            default: ;
        }
        if (isValid) {
            setLoading(true);
            switch (props.match.params.action) {
                case "register":
                    Post(urls.register,
                        { email: state.email, name: state.name, password: state.password },
                        context.lang).then(data => {
                            if (data)
                                setChEmail(true);
                            setLoading(false);
                        }).catch(() => props.history.push("/fatal"));
                    break;
                case "sign":
                    setTimeout(() => {
                        Post(urls.signInAsUser,
                            { email: state.email, password: state.password }, context.lang)
                            .then(data => {
                                let addr = null;
                                if (props.location.search.startsWith("?room="))
                                    addr = "/room/" + props.location.search.substring(6);
                                if (data) context.signInAsUser(data, addr);
                                else setLoading(false);
                            }).catch(() => props.history.push("/fatal"));
                    }, 1000);
                    break;
                case "recover":
                    Post(urls.recover, state.email, context.lang)
                        .then(data => {
                            if (data)
                                setRstPswd(true);
                            setLoading(false);
                        }).catch(() => props.history.push("/fatal"));
                    break;
                default: ;
            }
        }
    }
    const keyPressed = ev => {
        if (ev.which === 13)
            submit()
    }
    if (props.match.params.action !== "sign" &&
        props.match.params.action !== "register" &&
        props.match.params.action !== "recover") {
        props.history.replace("/signin/user/sign");
        return null;
    }
    text.setLanguage(context.lang);
    return chEmail
        ? <div className="loginframe">
            <h4 className="text-success text-center">{text.regHeader}</h4>
            <p className="mt-5 text-secondary">{text.regFirst}</p>
            <p className="text-secondary">{text.regSecond}</p>
        </div>
        : rstPswd
            ? <div className="loginframe">
                <h4 className="text-success text-center">{text.pswdSent}</h4>
            </div>
            : <div className="loginframe" onKeyPress={keyPressed} tabIndex="-1">
                <h5 className="text-secondary text-center mb-3">{text.header[props.match.params.action]}</h5>
                <div id="userbox" className="signbox">
                    {
                        text.inputs[props.match.params.action].map(([place, name]) =>
                            <div key={name}>
                                <input type={name === "password" || name === "confirm" ? "password" : name === "email" ? "email" : "text"}
                                    className="form-control" placeholder={place} name={name}
                                    onChange={name === "confirm" ? confirm : inputChange} />
                                <p className="text-danger"><small>{state[name + "Error"]}</small></p>
                            </div>)
                    }
                    {
                        props.match.params.action === "register" && context.jwt ?
                            <button onClick={submit} className="btn btn-block btn-primary">
                                <FontAwesomeIcon icon={text.buttons.register[0][2]} /> {text.buttons.register[0][0]}</button> :
                            text.buttons[props.match.params.action].map(([name, to, icon], index) =>
                                index === 0
                                    ? <button key={to} onClick={submit} className="btn btn-block btn-primary">
                                        <FontAwesomeIcon icon={icon} /> {name}</button>
                                    : <Link key={to} to={to + props.location.search} className="btn btn-block btn-outline-secondary">
                                        <FontAwesomeIcon icon={icon} /> {name}</Link>
                            )
                    }
                </div>
                {
                    loading && <Loading />
                }
            </div>
}