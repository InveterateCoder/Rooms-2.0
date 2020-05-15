import React, { useContext} from "react";
import { Context } from "../data/Context";
import { Link, Route, Switch, Redirect } from "react-router-dom";
import { AsGuestForm } from "./accessories/Login/AsGuestForm";
import { AsUserForm } from "./accessories/Login/AsUserForm";
import Nav from "react-bootstrap/Nav";
import LocalizedStrings from "react-localization";

const text = new LocalizedStrings({
    en: {
        msg: "Create your own chat room / Secure your room with a password / Send secret messages",
        Guest: "Guest",
        User: "User"
    },
    ru: {
        msg: "Создай собственную чат комнату / Защити свою комнату паролем / Отправляй секретные сообщения",
        Guest: "Гость",
        User: "Пользователь"
    }
})

export function SignIn(props) {
    const context = useContext(Context);
    const getStyle = caller => {
        if (caller === props.match.params.as)
            return { backgroundColor: "#fdfdfd" };
        else return {}
    }
    text.setLanguage(context.lang);
    const nav = <Nav justify variant="tabs" defaultActiveKey={props.match.params.as}>
        <Nav.Item>
            <Nav.Link style={getStyle("guest")} as={Link}
                to={`/signin/guest${props.location.search}`} eventKey="guest" replace>{text.Guest}</Nav.Link>
        </Nav.Item>
        <Nav.Item>
            <Nav.Link style={getStyle("user")} as={Link}
                to={`/signin/user/sign${props.location.search}`} eventKey="user" replace>{text.User}</Nav.Link>
        </Nav.Item>
    </Nav>
    const jumbo = <div className="jumbotron">
        <h2 className="display-4 font-weight-bold">Rooms</h2>
        <p className="text-info">{text.msg}</p>
    </div>
    const switchall = <Switch>
        <Route path="/signin/guest" exact={true} component={AsGuestForm} />
        <Route path="/signin/user/:action" exact={true} component={AsUserForm} />
        <Redirect to="/signin/guest" />
    </Switch>
    const switchreg = <Switch>
        <Route path="/signin/user/:action(register)" exact={true} component={AsUserForm} />
        <Redirect to="/signin/user/register" />
    </Switch>
    if (!context.jwt && !context.registered)
        return <div className="container">
            {jumbo}
            {nav}
            {switchall}
        </div>
    else if (context.jwt && !context.registered)
        return <div className="container">
            {jumbo}
            {switchreg}
        </div>
    props.history.replace("/lobby/1");
    return null;
}